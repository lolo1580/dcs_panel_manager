using System.Net;
using System.Net.Sockets;
using System.Text;
using DCSPanel.Core.Events;
using DCSPanel.Core.State;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.DCSBIOS.Protocol;
using Microsoft.Extensions.Logging;

namespace DCSPanel.DCSBIOS;

/// <summary>
/// UDP client for the official DCS-BIOS export and import protocols.
/// </summary>
public sealed partial class UdpDcsBiosClient : IDcsBiosClient
{
    private const ushort AircraftNameAddress = 0x0000;
    private const int AircraftNameLength = 24;
    private readonly ILogger<UdpDcsBiosClient> _logger;
    private readonly IActivitySink _activitySink;
    private readonly DcsBiosOptions _options;
    private readonly DcsBiosProtocolParser _parser = new();
    private readonly DcsBiosMemory _memory = new();
    private readonly object _outputSubscriptionGate = new();
    private IReadOnlyList<DcsBiosOutputSubscription> _outputSubscriptions = [];
    private readonly Dictionary<string, object?> _lastOutputValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private CancellationTokenSource? _lifetime;
    private UdpClient? _udpClient;
    private Task? _receiveTask;
    private Task? _monitorTask;
    private ConnectionStatus _status = ConnectionStatus.Disconnected;
    private string? _aircraft;
    private long _packetsReceived;
    private long _lastReceivedUnixMilliseconds;
    private long _lastMonitorActivityUnixMilliseconds;

    public UdpDcsBiosClient(
        ILogger<UdpDcsBiosClient> logger,
        IActivitySink activitySink,
        DcsBiosOptions options)
    {
        _logger = logger;
        _activitySink = activitySink;
        _options = options;
        _parser.WriteReceived += OnWriteReceived;
        _parser.FrameSynchronized += OnFrameSynchronized;
    }

    public event EventHandler<DcsBiosStateChanged>? StateChanged;
    public event EventHandler<DcsBiosValueChanged>? ValueChanged;
    public event EventHandler<DcsBiosDataReceived>? DataReceived;

    public ConnectionStatus Status => _status;
    public string? Aircraft => _aircraft;
    public long PacketsReceived => Interlocked.Read(ref _packetsReceived);

    public DateTimeOffset? LastReceivedAt
    {
        get
        {
            var value = Interlocked.Read(ref _lastReceivedUnixMilliseconds);
            return value == 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(value);
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lifetime is not null)
            {
                return;
            }

            SetState(ConnectionStatus.Connecting, null);
            var udpClient = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                udpClient.Client.ExclusiveAddressUse = false;
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _options.ReceivePort));
                udpClient.JoinMulticastGroup(_options.MulticastAddress);
            }
            catch
            {
                udpClient.Dispose();
                throw;
            }

            _parser.Reset();
            Interlocked.Exchange(ref _packetsReceived, 0);
            Interlocked.Exchange(ref _lastReceivedUnixMilliseconds, 0);
            _udpClient = udpClient;
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _receiveTask = Task.Run(() => ReceiveAsync(udpClient, _lifetime.Token), CancellationToken.None);
            _monitorTask = Task.Run(() => MonitorConnectionAsync(_lifetime.Token), CancellationToken.None);

            LogListenerStarted(_logger, _options.MulticastAddress, _options.ReceivePort);
            PublishActivity($"Listening on {_options.MulticastAddress}:{_options.ReceivePort} (read-only)");
        }
        catch (Exception exception)
        {
            SetState(ConnectionStatus.Faulted, null);
            LogListenerFailed(_logger, exception);
            PublishError(exception.Message);
            throw;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_lifetime is null)
            {
                SetState(ConnectionStatus.Disconnected, null);
                return;
            }

            await _lifetime.CancelAsync().ConfigureAwait(false);
            _udpClient?.Dispose();
            await AwaitStoppedTaskAsync(_receiveTask, cancellationToken).ConfigureAwait(false);
            await AwaitStoppedTaskAsync(_monitorTask, cancellationToken).ConfigureAwait(false);

            _receiveTask = null;
            _monitorTask = null;
            _udpClient = null;
            _lifetime.Dispose();
            _lifetime = null;
            _parser.Reset();
            SetState(ConnectionStatus.Disconnected, null);
            PublishActivity("Listener stopped");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public void SetOutputSubscriptions(IReadOnlyList<DcsBiosOutputSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        lock (_outputSubscriptionGate)
        {
            _outputSubscriptions = subscriptions
                .Where(subscription => !string.IsNullOrWhiteSpace(subscription.ControlId))
                .ToArray();
            _lastOutputValues.Clear();
        }
    }

    public async ValueTask SendCommandAsync(
        string controlId,
        string argument,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controlId);
        ArgumentException.ThrowIfNullOrWhiteSpace(argument);
        if (controlId.Any(char.IsWhiteSpace) || argument.Any(character => character is '\r' or '\n'))
            throw new ArgumentException("DCS-BIOS commands must be a single control and argument line.");

        using var sender = new UdpClient(AddressFamily.InterNetwork);
        var payload = Encoding.ASCII.GetBytes($"{controlId} {argument}\n");
        await sender.SendAsync(payload, new IPEndPoint(_options.CommandAddress, _options.CommandPort), cancellationToken).ConfigureAwait(false);
        PublishActivity($"TX {controlId} {argument}");
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        _parser.WriteReceived -= OnWriteReceived;
        _parser.FrameSynchronized -= OnFrameSynchronized;
        _lifecycleGate.Dispose();
    }

    private async Task ReceiveAsync(UdpClient udpClient, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                _parser.Process(result.Buffer);
                if (!_parser.IsSynchronized)
                {
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                Interlocked.Exchange(ref _lastReceivedUnixMilliseconds, now.ToUnixTimeMilliseconds());
                var packetNumber = Interlocked.Increment(ref _packetsReceived);
                if (_status != ConnectionStatus.Connected)
                {
                    SetState(ConnectionStatus.Connected, _aircraft);
                }

                DataReceived?.Invoke(this, new DcsBiosDataReceived(result.Buffer.Length, packetNumber, now));
                PublishSampledReceiveActivity(result.Buffer.Length, packetNumber, now);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (SocketException exception) when (cancellationToken.IsCancellationRequested &&
                                                exception.SocketErrorCode == SocketError.OperationAborted)
        {
        }
        catch (Exception exception)
        {
            SetState(ConnectionStatus.Faulted, _aircraft);
            LogReceiveFailed(_logger, exception);
            PublishError(exception.Message);
        }
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.MonitorInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                var lastReceived = LastReceivedAt;
                if (lastReceived is not null &&
                    DateTimeOffset.UtcNow - lastReceived > _options.InactivityTimeout &&
                    _status == ConnectionStatus.Connected)
                {
                    SetState(ConnectionStatus.Disconnected, null);
                    PublishActivity("No export data received; waiting for DCS-BIOS");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void OnWriteReceived(object? sender, DcsBiosWrite write) => _memory.Apply(write);

    private void OnFrameSynchronized(object? sender, EventArgs eventArgs)
    {
        var aircraft = _memory.ReadNullTerminatedAscii(AircraftNameAddress, AircraftNameLength);
        aircraft = string.IsNullOrWhiteSpace(aircraft) || aircraft.Equals("NONE", StringComparison.OrdinalIgnoreCase)
            ? null
            : aircraft;

        if (string.Equals(_aircraft, aircraft, StringComparison.Ordinal))
        {
            PublishConfiguredOutputValues();
            return;
        }

        var now = DateTimeOffset.UtcNow;
        ValueChanged?.Invoke(this, new DcsBiosValueChanged("_ACFT_NAME", aircraft, now));
        SetState(_status, aircraft);
        PublishActivity(aircraft is null ? "Aircraft session ended" : $"Aircraft detected: {aircraft}");
        PublishConfiguredOutputValues();
    }

    private void PublishConfiguredOutputValues()
    {
        List<DcsBiosValueChanged>? changed = null;
        var now = DateTimeOffset.UtcNow;
        lock (_outputSubscriptionGate)
        {
            foreach (var subscription in _outputSubscriptions)
            {
                object? value;
                try
                {
                    value = ReadOutputValue(subscription.Output);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }

                if (_lastOutputValues.TryGetValue(subscription.ControlId, out var previous) &&
                    Equals(previous, value))
                {
                    continue;
                }

                _lastOutputValues[subscription.ControlId] = value;
                (changed ??= []).Add(new DcsBiosValueChanged(subscription.ControlId, value, now));
            }
        }

        if (changed is not null)
        {
            foreach (var value in changed)
            {
                ValueChanged?.Invoke(this, value);
            }
        }
    }

    private object? ReadOutputValue(DcsBiosOutputMetadata output)
    {
        if (output.Address is not ushort address)
        {
            return null;
        }

        if (output.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return _memory.ReadNullTerminatedAscii(address, output.MaxLength ?? 1);
        }

        var value = _memory.ReadUnsignedWord(address);
        if (output.Mask is ushort mask)
        {
            value = (ushort)(value & mask);
        }

        return output.ShiftBy is int shift ? value >> shift : value;
    }

    private void SetState(ConnectionStatus status, string? aircraft)
    {
        if (_status == status && string.Equals(_aircraft, aircraft, StringComparison.Ordinal))
        {
            return;
        }

        _status = status;
        _aircraft = aircraft;
        StateChanged?.Invoke(this, new DcsBiosStateChanged(status, aircraft));
        LogStateChanged(_logger, status, aircraft ?? "none");
    }

    private void PublishSampledReceiveActivity(int byteCount, long packetNumber, DateTimeOffset now)
    {
        var timestamp = now.ToUnixTimeMilliseconds();
        var previous = Interlocked.Read(ref _lastMonitorActivityUnixMilliseconds);
        if (timestamp - previous < 250 ||
            Interlocked.CompareExchange(ref _lastMonitorActivityUnixMilliseconds, timestamp, previous) != previous)
        {
            return;
        }

        PublishActivity($"RX {byteCount} bytes (packet {packetNumber})");
    }

    private void PublishActivity(string message) =>
        _activitySink.Publish(new ActivityEvent(DateTimeOffset.Now, ActivityCategory.DcsBios, "UDP", message));

    private void PublishError(string message) =>
        _activitySink.Publish(new ActivityEvent(DateTimeOffset.Now, ActivityCategory.Error, "DCS-BIOS", message));

    private static async Task AwaitStoppedTaskAsync(Task? task, CancellationToken cancellationToken)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "DCS-BIOS listener started on {Address}:{Port}")]
    private static partial void LogListenerStarted(ILogger logger, IPAddress address, int port);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "DCS-BIOS listener failed to start")]
    private static partial void LogListenerFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "DCS-BIOS receive loop failed")]
    private static partial void LogReceiveFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "DCS-BIOS state changed to {Status}; aircraft {Aircraft}")]
    private static partial void LogStateChanged(ILogger logger, ConnectionStatus status, string aircraft);
}
