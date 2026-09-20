using System.Collections.Concurrent;
using DCSPanel.Core.Events;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Models;
using DCSPanel.Hardware.Logitech.Protocol;
using HidSharp;
using Microsoft.Extensions.Logging;

namespace DCSPanel.Hardware.Logitech;

public sealed partial class LogitechHidService(
    ILogger<LogitechHidService> logger,
    IActivitySink activitySink) : IHardwareService
{
    private readonly ConcurrentDictionary<string, ConnectedDevice> _connected = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _lifetime;
    private Task? _monitorTask;

    public event EventHandler<HardwareEvent>? HardwareEventOccurred;

    public IReadOnlyList<DeviceDescriptor> Devices => _connected.Values
        .Select(value => value.Descriptor)
        .OrderBy(value => value.Model, StringComparer.Ordinal)
        .ThenBy(value => value.InstancePath, StringComparer.Ordinal)
        .ToArray();

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_monitorTask is not null)
        {
            return Task.CompletedTask;
        }

        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _monitorTask = Task.Run(() => MonitorAsync(_lifetime.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_lifetime is null || _monitorTask is null)
        {
            return;
        }

        await _lifetime.CancelAsync().ConfigureAwait(false);
        try
        {
            await _monitorTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }

        foreach (var device in _connected.Values)
        {
            await device.DisposeAsync().ConfigureAwait(false);
        }

        _connected.Clear();
        _monitorTask = null;
        _lifetime.Dispose();
        _lifetime = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    public async Task SetOutputAsync(DeviceId deviceId, Output output, CancellationToken cancellationToken = default)
    {
        var connected = _connected.Values.FirstOrDefault(device => device.Descriptor.Id == deviceId);
        if (connected is null)
        {
            throw new InvalidOperationException("The selected panel is no longer connected.");
        }

        var reports = LogitechOutputEncoder.Encode(connected.Descriptor.Type, output);
        foreach (var report in reports)
        {
            await connected.SendOutputAsync(connected.Descriptor.Type, report, cancellationToken).ConfigureAwait(false);
        }

        activitySink.Publish(new ActivityEvent(
            DateTimeOffset.Now,
            ActivityCategory.Hardware,
            connected.Descriptor.Type.ToString(),
            $"TX {output.Id}"));
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        activitySink.Publish(new ActivityEvent(DateTimeOffset.Now, ActivityCategory.Hardware, "HID", "Panel monitoring started"));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ScanAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogEnumerationFailure(logger, exception);
                activitySink.Publish(new ActivityEvent(DateTimeOffset.Now, ActivityCategory.Error, "HID", exception.Message));
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        var discovered = DeviceList.Local.GetHidDevices()
            .Where(device => LogitechDeviceDefinitions.TryIdentify(device.VendorID, device.ProductID, out _, out _))
            .ToDictionary(device => device.DevicePath, StringComparer.OrdinalIgnoreCase);

        foreach (var missingPath in _connected.Keys.Except(discovered.Keys, StringComparer.OrdinalIgnoreCase).ToArray())
        {
            if (!_connected.TryRemove(missingPath, out var removed))
            {
                continue;
            }

            removed.Cancel();
            await removed.DisposeAsync().ConfigureAwait(false);
            var descriptor = removed.Descriptor with { State = DeviceConnectionState.Disconnected };
            Publish(new HardwareEvent(HardwareEventKind.Disconnected, descriptor));
        }

        foreach (var (path, hidDevice) in discovered)
        {
            if (_connected.ContainsKey(path))
            {
                continue;
            }

            LogitechDeviceDefinitions.TryIdentify(hidDevice.VendorID, hidDevice.ProductID, out var type, out var model);
            var serial = TryGetSerialNumber(hidDevice);
            var id = new DeviceId(!string.IsNullOrWhiteSpace(serial) ? $"{type}:{serial}:{path}" : $"{type}:{path}");
            var descriptor = new DeviceDescriptor(
                id, type, model, hidDevice.VendorID, hidDevice.ProductID, path, serial,
                DeviceConnectionState.Connected, DateTimeOffset.Now);
            var connected = new ConnectedDevice(descriptor, cancellationToken);

            if (!_connected.TryAdd(path, connected))
            {
                await connected.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            Publish(new HardwareEvent(HardwareEventKind.Connected, descriptor));
            connected.ReaderTask = Task.Run(() => ReadReportsAsync(hidDevice, connected), CancellationToken.None);
        }
    }

    private async Task ReadReportsAsync(HidDevice hidDevice, ConnectedDevice connected)
    {
        try
        {
            if (!hidDevice.TryOpen(out var stream))
            {
                throw new IOException("Unable to open the HID device.");
            }

            connected.Stream = stream;
            var buffer = new byte[hidDevice.GetMaxInputReportLength()];
            byte[] previous = [];

            while (!connected.Token.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer.AsMemory(), connected.Token).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    // HidSharp uses a three-second timeout by default. A silent panel
                    // is still connected, so resume waiting for the next report.
                    continue;
                }

                if (bytesRead <= 0)
                {
                    continue;
                }

                var report = NormalizeReport(buffer.AsSpan(0, bytesRead));
                var now = DateTimeOffset.Now;
                var raw = new RawInputReport(connected.Descriptor.Id, connected.Descriptor.Type, report, now);
                Publish(new HardwareEvent(HardwareEventKind.RawInput, connected.Descriptor, raw));

                foreach (var input in LogitechInputDecoder.DecodeChanges(connected.Descriptor.Id, connected.Descriptor.Type, previous, report.Span, now))
                {
                    Publish(new HardwareEvent(HardwareEventKind.Input, connected.Descriptor, Input: input));
                }

                previous = report.ToArray();
            }
        }
        catch (OperationCanceledException) when (connected.Token.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (connected.Token.IsCancellationRequested)
        {
            // Normal stream closure during shutdown or device removal.
        }
        catch (Exception exception)
        {
            LogReadFailure(logger, connected.Descriptor.Id.ToString(), exception);
            Publish(new HardwareEvent(HardwareEventKind.Error, connected.Descriptor, Exception: exception));
        }
    }

    private static ReadOnlyMemory<byte> NormalizeReport(ReadOnlySpan<byte> report)
    {
        // HidSharp may include the Report ID byte (0x00). PZ55/PZ70 input payloads contain three useful bytes.
        var payload = report.Length >= 4 && report[0] == 0 ? report[1..] : report;
        return payload[..Math.Min(payload.Length, 3)].ToArray();
    }

    private static string? TryGetSerialNumber(HidDevice device)
    {
        try
        {
            return device.GetSerialNumber();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void Publish(HardwareEvent hardwareEvent)
    {
        HardwareEventOccurred?.Invoke(this, hardwareEvent);
        switch (hardwareEvent.Kind)
        {
            case HardwareEventKind.Connected:
                LogDeviceConnected(logger, hardwareEvent.Device.Model, hardwareEvent.Device.InstancePath);
                break;
            case HardwareEventKind.Disconnected:
                LogDeviceDisconnected(logger, hardwareEvent.Device.Model, hardwareEvent.Device.InstancePath);
                break;
            case HardwareEventKind.RawInput:
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var deviceType = hardwareEvent.Device.Type.ToString();
                    var report = Convert.ToHexString(hardwareEvent.RawReport!.Data.Span);
                    LogRawReport(logger, deviceType, report);
                }
                break;
            case HardwareEventKind.Input:
                if (logger.IsEnabled(LogLevel.Information))
                {
                    var deviceType = hardwareEvent.Device.Type.ToString();
                    var input = hardwareEvent.Input!;
                    var kind = input.Kind.ToString();
                    LogInputEvent(logger, deviceType, input.ControlId, kind);
                }
                break;
        }

        var message = hardwareEvent.Kind switch
        {
            HardwareEventKind.Connected => $"Connected - {hardwareEvent.Device.Model}",
            HardwareEventKind.Disconnected => $"Disconnected - {hardwareEvent.Device.Model}",
            HardwareEventKind.RawInput => $"RX {Convert.ToHexString(hardwareEvent.RawReport!.Data.Span)}",
            HardwareEventKind.Input => $"{hardwareEvent.Input!.ControlId} - {hardwareEvent.Input.Kind}",
            HardwareEventKind.Error => hardwareEvent.Exception?.Message ?? "HID error",
            _ => hardwareEvent.Kind.ToString()
        };
        activitySink.Publish(new ActivityEvent(
            DateTimeOffset.Now,
            hardwareEvent.Kind == HardwareEventKind.Error ? ActivityCategory.Error : ActivityCategory.Hardware,
            hardwareEvent.Device.Type.ToString(),
            message));
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Failed to enumerate Logitech HID devices")]
    private static partial void LogEnumerationFailure(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "HID read failed for {DeviceId}")]
    private static partial void LogReadFailure(ILogger logger, string deviceId, Exception exception);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Panel connected: {Model} ({InstancePath})")]
    private static partial void LogDeviceConnected(ILogger logger, string model, string instancePath);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Panel disconnected: {Model} ({InstancePath})")]
    private static partial void LogDeviceDisconnected(ILogger logger, string model, string instancePath);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Debug, Message = "HID report {DeviceType}: {Report}", SkipEnabledCheck = true)]
    private static partial void LogRawReport(ILogger logger, string deviceType, string report);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Input {DeviceType}: {ControlId} {Kind}", SkipEnabledCheck = true)]
    private static partial void LogInputEvent(ILogger logger, string deviceType, string controlId, string kind);

    private sealed class ConnectedDevice(DeviceDescriptor descriptor, CancellationToken parentToken) : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public DeviceDescriptor Descriptor { get; } = descriptor;
        public CancellationToken Token => _cancellation.Token;
        public HidStream? Stream { get; set; }
        public Task? ReaderTask { get; set; }

        public void Cancel() => _cancellation.Cancel();

        public async Task SendOutputAsync(DeviceType deviceType, byte[] report, CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Stream is null)
                {
                    throw new InvalidOperationException("The panel is still opening. Try again in a moment.");
                }

                Stream.SetFeature(report);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cancellation.CancelAsync().ConfigureAwait(false);
            Stream?.Dispose();
            if (ReaderTask is not null)
            {
                try
                {
                    await ReaderTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            _cancellation.Dispose();
            _writeLock.Dispose();
        }
    }
}
