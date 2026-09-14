using System.Collections.ObjectModel;
using Avalonia.Threading;
using DCSPanel.Core.Events;
using DCSPanel.Core.State;
using DCSPanel.DCSBIOS;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Models;

namespace DCSPanel.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaximumActivityCount = 500;
    private readonly IHardwareService _hardwareService;
    private readonly IActivitySink _activitySink;
    private readonly IDcsBiosClient _dcsBiosClient;
    private readonly IDcsBiosMetadataProvider _metadataProvider;
    private readonly string _dcsBiosEndpoint;
    private readonly List<ActivityEventViewModel> _allActivities = [];
    private string _dcsWorldStatus = "Disconnected";
    private string _dcsBiosStatus = "Disconnected";
    private string _detectedAircraft = "-";
    private string _metadataStatus;
    private long _dcsBiosPacketCount;
    private string _lastDcsBiosData = "No data received";
    private bool _showHardware = true;
    private bool _showMapping = true;
    private bool _showDcsBios = true;
    private bool _showErrors = true;

    public MainWindowViewModel(
        IHardwareService hardwareService,
        IActivitySink activitySink,
        IDcsBiosClient dcsBiosClient,
        IDcsBiosMetadataProvider metadataProvider,
        DcsBiosOptions dcsBiosOptions)
    {
        _hardwareService = hardwareService;
        _activitySink = activitySink;
        _dcsBiosClient = dcsBiosClient;
        _metadataProvider = metadataProvider;
        _dcsBiosEndpoint = $"{dcsBiosOptions.MulticastAddress}:{dcsBiosOptions.ReceivePort}";
        _metadataStatus = metadataProvider.MetadataDirectory is null
            ? "Not found in Saved Games"
            : "Ready";
        _hardwareService.HardwareEventOccurred += OnHardwareEvent;
        _activitySink.ActivityPublished += OnActivityPublished;
        _dcsBiosClient.StateChanged += OnDcsBiosStateChanged;
        _dcsBiosClient.DataReceived += OnDcsBiosDataReceived;
        ActiveProfile = "Generic";
    }

    public ObservableCollection<DeviceViewModel> Devices { get; } = [];
    public ObservableCollection<ActivityEventViewModel> Activities { get; } = [];

    public string DcsWorldStatus
    {
        get => _dcsWorldStatus;
        private set => SetProperty(ref _dcsWorldStatus, value);
    }

    public string DcsBiosStatus
    {
        get => _dcsBiosStatus;
        private set => SetProperty(ref _dcsBiosStatus, value);
    }

    public string DetectedAircraft
    {
        get => _detectedAircraft;
        private set => SetProperty(ref _detectedAircraft, value);
    }

    public string MetadataStatus
    {
        get => _metadataStatus;
        private set => SetProperty(ref _metadataStatus, value);
    }

    public long DcsBiosPacketCount
    {
        get => _dcsBiosPacketCount;
        private set => SetProperty(ref _dcsBiosPacketCount, value);
    }

    public string LastDcsBiosData
    {
        get => _lastDcsBiosData;
        private set => SetProperty(ref _lastDcsBiosData, value);
    }

    public string DcsBiosEndpoint => _dcsBiosEndpoint;
    public string MetadataDirectory => _metadataProvider.MetadataDirectory ?? "DCS-BIOS is not installed in Saved Games";
    public string ActiveProfile { get; }
    public string DeviceSummary => Devices.Count == 0 ? "No panels detected" : $"{Devices.Count} panel(s) connected";
    public bool HasNoDevices => Devices.Count == 0;

    public bool ShowHardware
    {
        get => _showHardware;
        set { if (SetProperty(ref _showHardware, value)) RefreshActivities(); }
    }

    public bool ShowMapping
    {
        get => _showMapping;
        set { if (SetProperty(ref _showMapping, value)) RefreshActivities(); }
    }

    public bool ShowDcsBios
    {
        get => _showDcsBios;
        set { if (SetProperty(ref _showDcsBios, value)) RefreshActivities(); }
    }

    public bool ShowErrors
    {
        get => _showErrors;
        set { if (SetProperty(ref _showErrors, value)) RefreshActivities(); }
    }

    private void OnHardwareEvent(object? sender, HardwareEvent hardwareEvent)
    {
        if (hardwareEvent.Kind is not (HardwareEventKind.Connected or HardwareEventKind.Disconnected))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var existing = Devices.FirstOrDefault(device => device.Id == hardwareEvent.Device.Id);
            if (existing is not null)
            {
                Devices.Remove(existing);
            }

            if (hardwareEvent.Kind == HardwareEventKind.Connected)
            {
                Devices.Add(new DeviceViewModel(hardwareEvent.Device));
            }

            RaisePropertyChanged(nameof(DeviceSummary));
            RaisePropertyChanged(nameof(HasNoDevices));
        });
    }

    private void OnActivityPublished(object? sender, ActivityEvent activity)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var item = new ActivityEventViewModel(activity);
            _allActivities.Insert(0, item);
            if (_allActivities.Count > MaximumActivityCount)
            {
                _allActivities.RemoveAt(_allActivities.Count - 1);
            }

            if (IsVisible(item.CategoryValue))
            {
                Activities.Insert(0, item);
                if (Activities.Count > MaximumActivityCount)
                {
                    Activities.RemoveAt(Activities.Count - 1);
                }
            }
        });
    }

    private void OnDcsBiosStateChanged(object? sender, DcsBiosStateChanged state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DcsBiosStatus = state.Status switch
            {
                ConnectionStatus.Connecting => "Waiting for data",
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Faulted => "Faulted",
                _ => "Disconnected"
            };
            DcsWorldStatus = state.Status == ConnectionStatus.Connected ? "Connected" : "Disconnected";
            DetectedAircraft = state.Aircraft ?? "-";
            if (state.Aircraft is not null)
            {
                _ = LoadMetadataAsync(state.Aircraft);
            }
        });
    }

    private void OnDcsBiosDataReceived(object? sender, DcsBiosDataReceived data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DcsBiosPacketCount = data.PacketNumber;
            LastDcsBiosData = $"{data.Timestamp.ToLocalTime():HH:mm:ss} - {data.ByteCount} bytes";
        });
    }

    private async Task LoadMetadataAsync(string aircraft)
    {
        try
        {
            var controls = await _metadataProvider.GetControlsAsync(aircraft).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() => MetadataStatus = controls.Count == 0
                ? $"No metadata found for {aircraft}"
                : $"{controls.Count} controls loaded for {aircraft}");
        }
        catch (Exception exception)
        {
            _activitySink.Publish(new ActivityEvent(
                DateTimeOffset.Now,
                ActivityCategory.Error,
                "DCS-BIOS metadata",
                exception.Message));
            Dispatcher.UIThread.Post(() => MetadataStatus = "Metadata loading failed");
        }
    }

    private void RefreshActivities()
    {
        Activities.Clear();
        foreach (var item in _allActivities.Where(item => IsVisible(item.CategoryValue)))
        {
            Activities.Add(item);
        }
    }

    private bool IsVisible(ActivityCategory category) => category switch
    {
        ActivityCategory.Hardware => ShowHardware,
        ActivityCategory.Mapping => ShowMapping,
        ActivityCategory.DcsBios => ShowDcsBios,
        ActivityCategory.Error => ShowErrors,
        _ => true
    };

    public void Dispose()
    {
        _hardwareService.HardwareEventOccurred -= OnHardwareEvent;
        _activitySink.ActivityPublished -= OnActivityPublished;
        _dcsBiosClient.StateChanged -= OnDcsBiosStateChanged;
        _dcsBiosClient.DataReceived -= OnDcsBiosDataReceived;
    }
}
