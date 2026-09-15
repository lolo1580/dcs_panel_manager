using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using DCSPanel.Core.Events;
using DCSPanel.Core.State;
using DCSPanel.DCSBIOS;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Models;
using DCSPanel.Profiles;

namespace DCSPanel.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaximumActivityCount = 500;
    private readonly IHardwareService _hardwareService;
    private readonly IActivitySink _activitySink;
    private readonly IDcsBiosClient _dcsBiosClient;
    private readonly IDcsBiosMetadataProvider _metadataProvider;
    private readonly ProfileCatalog _profileCatalog;
    private readonly string _dcsBiosEndpoint;
    private readonly string _profileDirectory;
    private readonly List<ActivityEventViewModel> _allActivities = [];
    private readonly List<DcsBiosControlViewModel> _allControls = [];
    private CancellationTokenSource? _metadataLoadCancellation;
    private string _dcsWorldStatus = "Disconnected";
    private string _dcsBiosStatus = "Disconnected";
    private IBrush _dcsWorldStatusBrush = Brush.Parse("#F87171");
    private IBrush _dcsBiosStatusBrush = Brush.Parse("#F87171");
    private string _detectedAircraft = "-";
    private string _metadataStatus;
    private long _dcsBiosPacketCount;
    private string _lastDcsBiosData = "No data received";
    private string _activeProfile = "Loading...";
    private string _controlCatalogAircraft = "No aircraft detected";
    private string _controlSearchText = string.Empty;
    private ProfileViewModel? _selectedProfile;
    private bool _showHardware = true;
    private bool _showMapping = true;
    private bool _showDcsBios = true;
    private bool _showErrors = true;

    public MainWindowViewModel(
        IHardwareService hardwareService,
        IActivitySink activitySink,
        IDcsBiosClient dcsBiosClient,
        IDcsBiosMetadataProvider metadataProvider,
        DcsBiosOptions dcsBiosOptions,
        ProfileCatalog profileCatalog)
    {
        _hardwareService = hardwareService;
        _activitySink = activitySink;
        _dcsBiosClient = dcsBiosClient;
        _metadataProvider = metadataProvider;
        _profileCatalog = profileCatalog;
        _dcsBiosEndpoint = $"{dcsBiosOptions.MulticastAddress}:{dcsBiosOptions.ReceivePort}";
        _profileDirectory = Path.Combine(AppContext.BaseDirectory, "profiles");
        _metadataStatus = metadataProvider.MetadataDirectory is null
            ? "Not found in Saved Games"
            : "Ready";
        _hardwareService.HardwareEventOccurred += OnHardwareEvent;
        _activitySink.ActivityPublished += OnActivityPublished;
        _dcsBiosClient.StateChanged += OnDcsBiosStateChanged;
        _dcsBiosClient.DataReceived += OnDcsBiosDataReceived;
    }

    public ObservableCollection<DeviceViewModel> Devices { get; } = [];
    public ObservableCollection<ActivityEventViewModel> Activities { get; } = [];
    public ObservableCollection<ProfileViewModel> Profiles { get; } = [];
    public ObservableCollection<MappingViewModel> Mappings { get; } = [];
    public ObservableCollection<DcsBiosControlViewModel> Controls { get; } = [];

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

    public IBrush DcsWorldStatusBrush
    {
        get => _dcsWorldStatusBrush;
        private set => SetProperty(ref _dcsWorldStatusBrush, value);
    }

    public IBrush DcsBiosStatusBrush
    {
        get => _dcsBiosStatusBrush;
        private set => SetProperty(ref _dcsBiosStatusBrush, value);
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
    public string ControlCatalogAircraft
    {
        get => _controlCatalogAircraft;
        private set => SetProperty(ref _controlCatalogAircraft, value);
    }

    public string ControlSearchText
    {
        get => _controlSearchText;
        set
        {
            if (SetProperty(ref _controlSearchText, value))
            {
                RefreshControlFilter();
            }
        }
    }

    public string ControlCountSummary => _allControls.Count == Controls.Count
        ? $"{Controls.Count} controls"
        : $"{Controls.Count} of {_allControls.Count} controls";
    public string ActiveProfile
    {
        get => _activeProfile;
        private set => SetProperty(ref _activeProfile, value);
    }

    public ProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value) && value is not null)
            {
                ActiveProfile = value.DisplayName;
                RefreshMappings(value);
                RaisePropertyChanged(nameof(HasNoMappings));
            }
        }
    }

    public bool HasNoProfiles => Profiles.Count == 0;
    public bool HasNoMappings => Mappings.Count == 0;
    public bool HasNoControls => Controls.Count == 0;
    public string ProfileDirectory => _profileDirectory;
    public string DeviceSummary => Devices.Count == 0 ? "No panels detected" : $"{Devices.Count} panel(s) connected";
    public bool HasNoDevices => Devices.Count == 0;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var result = await _profileCatalog.LoadDirectoryAsync(ProfileDirectory, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<string> aircraft = [];
        try
        {
            aircraft = await _metadataProvider.GetAircraftAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            _activitySink.Publish(new ActivityEvent(
                DateTimeOffset.Now,
                ActivityCategory.Error,
                "DCS-BIOS metadata",
                $"Unable to load aircraft aliases: {exception.Message}"));
        }

        var profiles = result.Profiles.ToList();
        var definedAircraft = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.AircraftModule))
            .Select(profile => profile.AircraftModule!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        profiles.AddRange(aircraft
            .Where(aircraftModule => !definedAircraft.Contains(aircraftModule))
            .Select(StarterProfileFactory.Create));

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Profiles.Clear();
            foreach (var profile in profiles
                         .OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
                         .Select(profile => new ProfileViewModel(profile)))
            {
                Profiles.Add(profile);
            }

            if (aircraft.Count > 0)
            {
                MetadataStatus = $"{aircraft.Count} aircraft definitions available";
            }

            RaisePropertyChanged(nameof(HasNoProfiles));
            SelectProfileForAircraft(_dcsBiosClient.Aircraft);
        });

        foreach (var error in result.Errors)
        {
            _activitySink.Publish(new ActivityEvent(
                DateTimeOffset.Now,
                ActivityCategory.Error,
                "Profiles",
                $"{Path.GetFileName(error.Path)}: {error.Message}"));
        }
    }

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
            DcsBiosStatusBrush = state.Status switch
            {
                ConnectionStatus.Connected => Brush.Parse("#34D399"),
                ConnectionStatus.Connecting => Brush.Parse("#FBBF24"),
                _ => Brush.Parse("#F87171")
            };
            DcsWorldStatusBrush = state.Status == ConnectionStatus.Connected
                ? Brush.Parse("#34D399")
                : Brush.Parse("#F87171");
            DetectedAircraft = state.Aircraft ?? "-";
            SelectProfileForAircraft(state.Aircraft);
            _metadataLoadCancellation?.Cancel();
            _metadataLoadCancellation?.Dispose();
            _metadataLoadCancellation = null;
            if (state.Aircraft is not null)
            {
                _metadataLoadCancellation = new CancellationTokenSource();
                ControlCatalogAircraft = state.Aircraft;
                MetadataStatus = $"Loading controls for {state.Aircraft}...";
                _ = LoadMetadataAsync(state.Aircraft, _metadataLoadCancellation.Token);
            }
            else
            {
                ControlCatalogAircraft = "No aircraft detected";
                _allControls.Clear();
                Controls.Clear();
                RaisePropertyChanged(nameof(ControlCountSummary));
                RaisePropertyChanged(nameof(HasNoControls));
            }
        });
    }

    private void SelectProfileForAircraft(string? aircraft)
    {
        var selected = ProfileCatalog.SelectForAircraft(
            Profiles.Select(profile => profile.Profile),
            aircraft);
        if (selected is null)
        {
            SelectedProfile = null;
            ActiveProfile = "None";
            Mappings.Clear();
            RaisePropertyChanged(nameof(HasNoMappings));
            return;
        }

        SelectedProfile = Profiles.First(profile => ReferenceEquals(profile.Profile, selected));
    }

    private void RefreshMappings(ProfileViewModel profile)
    {
        Mappings.Clear();
        foreach (var device in profile.Profile.Devices)
        {
            foreach (var mapping in device.Mappings)
            {
                foreach (var action in mapping.Actions)
                {
                    Mappings.Add(new MappingViewModel(
                        device.DeviceType,
                        mapping.ControlId,
                        mapping.Kind,
                        action));
                }
            }
        }
    }

    private void OnDcsBiosDataReceived(object? sender, DcsBiosDataReceived data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DcsBiosPacketCount = data.PacketNumber;
            LastDcsBiosData = $"{data.Timestamp.ToLocalTime():HH:mm:ss} - {data.ByteCount} bytes";
        });
    }

    private async Task LoadMetadataAsync(string aircraft, CancellationToken cancellationToken)
    {
        try
        {
            var controls = await _metadataProvider.GetControlsAsync(aircraft, cancellationToken).ConfigureAwait(false);
            Dispatcher.UIThread.Post(() =>
            {
                if (cancellationToken.IsCancellationRequested ||
                    !string.Equals(_dcsBiosClient.Aircraft, aircraft, StringComparison.Ordinal))
                {
                    return;
                }

                _allControls.Clear();
                _allControls.AddRange(controls.Select(control => new DcsBiosControlViewModel(control)));
                RefreshControlFilter();
                MetadataStatus = controls.Count == 0
                    ? $"No metadata found for {aircraft}"
                    : $"{controls.Count} controls loaded for {aircraft}";
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
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

    private void RefreshControlFilter()
    {
        Controls.Clear();
        foreach (var control in _allControls.Where(control => control.Matches(ControlSearchText)))
        {
            Controls.Add(control);
        }

        RaisePropertyChanged(nameof(ControlCountSummary));
        RaisePropertyChanged(nameof(HasNoControls));
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
        _metadataLoadCancellation?.Cancel();
        _metadataLoadCancellation?.Dispose();
        _hardwareService.HardwareEventOccurred -= OnHardwareEvent;
        _activitySink.ActivityPublished -= OnActivityPublished;
        _dcsBiosClient.StateChanged -= OnDcsBiosStateChanged;
        _dcsBiosClient.DataReceived -= OnDcsBiosDataReceived;
    }
}
