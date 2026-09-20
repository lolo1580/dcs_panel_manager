using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Threading;
using DCSPanel.Core.Events;
using DCSPanel.Core.Mapping;
using DCSPanel.Core.State;
using DCSPanel.DCSBIOS;
using DCSPanel.DCSBIOS.Abstractions;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Models;
using DCSPanel.Profiles;
using DCSPanel.Profiles.Models;

namespace DCSPanel.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaximumActivityCount = 500;
    private readonly IHardwareService _hardwareService;
    private readonly IActivitySink _activitySink;
    private readonly IDcsBiosClient _dcsBiosClient;
    private readonly IDcsBiosMetadataProvider _metadataProvider;
    private readonly ProfileCatalog _profileCatalog;
    private readonly JsonProfileRepository _profileRepository;
    private readonly string _dcsBiosEndpoint;
    private readonly string _profileDirectory;
    private readonly List<ActivityEventViewModel> _allActivities = [];
    private readonly List<DcsBiosControlViewModel> _allControls = [];
    private CancellationTokenSource? _metadataLoadCancellation;
    private int _isLiveMonitorSessionActive;
    private bool _isSavingProfile;
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
    private string _learnedDeviceType = "No input learned";
    private string _learnedControlId = "Press Learn input, then operate a panel control";
    private string _mappingArgument = string.Empty;
    private string _mappingCommandSearchText = string.Empty;
    private string _editorStatus = "Ready to create a safe mapping";
    private bool _isLearningInput;
    private DcsBiosControlViewModel? _selectedCommandControl;
    private MappingTriggerOption? _selectedTriggerOption;
    private MappingViewModel? _selectedMapping;
    private DcsBiosControlViewModel? _selectedOutputControl;
    private OutputTargetOption? _selectedOutputTarget;
    private OutputBindingViewModel? _selectedOutputBinding;
    private PanelOutputColor _selectedOutputColor = PanelOutputColor.Green;
    private Pz55GearLights _pz55Output = new(Pz55GearLightColor.Off, Pz55GearLightColor.Off, Pz55GearLightColor.Off);
    private Pz70PanelOutput _pz70Output = new(null, null);
    private ProfileViewModel? _selectedProfile;
    private bool _showHardware = true;
    private bool _showMapping = true;
    private bool _showDcsBios = true;
    private bool _showErrors = true;
    private bool _isCommandTransmissionEnabled;

    public MainWindowViewModel(
        IHardwareService hardwareService,
        IActivitySink activitySink,
        IDcsBiosClient dcsBiosClient,
        IDcsBiosMetadataProvider metadataProvider,
        DcsBiosOptions dcsBiosOptions,
        ProfileCatalog profileCatalog,
        JsonProfileRepository profileRepository)
    {
        _hardwareService = hardwareService;
        _activitySink = activitySink;
        _dcsBiosClient = dcsBiosClient;
        _metadataProvider = metadataProvider;
        _profileCatalog = profileCatalog;
        _profileRepository = profileRepository;
        _dcsBiosEndpoint = $"{dcsBiosOptions.MulticastAddress}:{dcsBiosOptions.ReceivePort}";
        _profileDirectory = Path.Combine(AppContext.BaseDirectory, "profiles");
        _metadataStatus = metadataProvider.MetadataDirectory is null
            ? "Not found in Saved Games"
            : "Ready";
        _hardwareService.HardwareEventOccurred += OnHardwareEvent;
        _activitySink.ActivityPublished += OnActivityPublished;
        _dcsBiosClient.StateChanged += OnDcsBiosStateChanged;
        _dcsBiosClient.ValueChanged += OnDcsBiosValueChanged;
        _dcsBiosClient.DataReceived += OnDcsBiosDataReceived;
        LearnInputCommand = new RelayCommand(StartLearningInput);
        AddMappingCommand = new AsyncRelayCommand(AddMappingAsync, CanAddMapping);
        TestMappingCommand = new RelayCommand(TestMapping, CanAddMapping);
        RemoveMappingCommand = new AsyncRelayCommand(RemoveSelectedMappingAsync, () => !_isSavingProfile && SelectedMapping is not null);
        AddOutputBindingCommand = new AsyncRelayCommand(AddOutputBindingAsync, CanAddOutputBinding);
        RemoveOutputBindingCommand = new AsyncRelayCommand(RemoveSelectedOutputBindingAsync, () => !_isSavingProfile && SelectedOutputBinding is not null);
        foreach (var target in Enum.GetValues<PanelOutputTarget>())
        {
            OutputTargets.Add(new OutputTargetOption(target, target.ToString()));
        }
    }

    public ObservableCollection<DeviceViewModel> Devices { get; } = [];
    public ObservableCollection<ActivityEventViewModel> Activities { get; } = [];
    public ObservableCollection<ProfileViewModel> Profiles { get; } = [];
    public ObservableCollection<MappingViewModel> Mappings { get; } = [];
    public ObservableCollection<DcsBiosControlViewModel> Controls { get; } = [];
    public ObservableCollection<DcsBiosControlViewModel> WritableControls { get; } = [];
    public ObservableCollection<MappingTriggerOption> TriggerOptions { get; } = [];
    public ObservableCollection<DcsBiosControlViewModel> OutputControls { get; } = [];
    public ObservableCollection<OutputTargetOption> OutputTargets { get; } = [];
    public ObservableCollection<PanelOutputColor> OutputColors { get; } = [PanelOutputColor.Green, PanelOutputColor.Red, PanelOutputColor.Yellow];
    public ObservableCollection<OutputBindingViewModel> OutputBindings { get; } = [];

    public RelayCommand LearnInputCommand { get; }
    public AsyncRelayCommand AddMappingCommand { get; }
    public RelayCommand TestMappingCommand { get; }
    public AsyncRelayCommand RemoveMappingCommand { get; }
    public AsyncRelayCommand AddOutputBindingCommand { get; }
    public AsyncRelayCommand RemoveOutputBindingCommand { get; }

    public DcsBiosControlViewModel? SelectedOutputControl { get => _selectedOutputControl; set { if (SetProperty(ref _selectedOutputControl, value)) AddOutputBindingCommand.RaiseCanExecuteChanged(); } }
    public OutputTargetOption? SelectedOutputTarget { get => _selectedOutputTarget; set { if (SetProperty(ref _selectedOutputTarget, value)) AddOutputBindingCommand.RaiseCanExecuteChanged(); } }
    public PanelOutputColor SelectedOutputColor { get => _selectedOutputColor; set => SetProperty(ref _selectedOutputColor, value); }
    public OutputBindingViewModel? SelectedOutputBinding { get => _selectedOutputBinding; set { if (SetProperty(ref _selectedOutputBinding, value)) RemoveOutputBindingCommand.RaiseCanExecuteChanged(); } }
    public bool HasNoOutputBindings => OutputBindings.Count == 0;

    public void BeginLiveMonitorSession()
    {
        _allActivities.Clear();
        Activities.Clear();
        Volatile.Write(ref _isLiveMonitorSessionActive, 1);
    }

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
    public string LearnedDeviceType
    {
        get => _learnedDeviceType;
        private set => SetProperty(ref _learnedDeviceType, value);
    }

    public string LearnedControlId
    {
        get => _learnedControlId;
        private set => SetProperty(ref _learnedControlId, value);
    }

    public string LearnInputLabel => IsLearningInput ? "Waiting for panel input..." : "Learn input";

    public bool IsLearningInput
    {
        get => _isLearningInput;
        private set
        {
            if (SetProperty(ref _isLearningInput, value))
            {
                RaisePropertyChanged(nameof(LearnInputLabel));
            }
        }
    }

    public DcsBiosControlViewModel? SelectedCommandControl
    {
        get => _selectedCommandControl;
        set
        {
            if (SetProperty(ref _selectedCommandControl, value))
            {
                SuggestMappingArgument();
                RefreshEditorCommands();
            }
        }
    }

    public MappingTriggerOption? SelectedTriggerOption
    {
        get => _selectedTriggerOption;
        set
        {
            if (SetProperty(ref _selectedTriggerOption, value))
            {
                SuggestMappingArgument();
                RefreshEditorCommands();
            }
        }
    }

    public string MappingArgument
    {
        get => _mappingArgument;
        set
        {
            if (SetProperty(ref _mappingArgument, value))
            {
                RefreshEditorCommands();
            }
        }
    }

    public string MappingCommandSearchText
    {
        get => _mappingCommandSearchText;
        set
        {
            if (SetProperty(ref _mappingCommandSearchText, value))
            {
                RefreshWritableControlFilter();
            }
        }
    }

    public string EditorStatus
    {
        get => _editorStatus;
        private set => SetProperty(ref _editorStatus, value);
    }

    public MappingViewModel? SelectedMapping
    {
        get => _selectedMapping;
        set
        {
            if (SetProperty(ref _selectedMapping, value))
            {
                RemoveMappingCommand.RaiseCanExecuteChanged();
            }
        }
    }
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
            if (!SetProperty(ref _selectedProfile, value))
            {
                return;
            }

            if (value is not null)
            {
                ActiveProfile = value.DisplayName;
                RefreshMappings(value);
                RefreshOutputBindings(value);
                ResetPanelOutputs();
                ConfigureOutputSubscriptions();
                RaisePropertyChanged(nameof(HasNoMappings));
            }

            RefreshEditorCommands();
        }
    }

    public bool HasNoProfiles => Profiles.Count == 0;
    public bool IsCommandTransmissionEnabled
    {
        get => _isCommandTransmissionEnabled;
        set
        {
            if (SetProperty(ref _isCommandTransmissionEnabled, value))
            {
                RaisePropertyChanged(nameof(CommandTransmissionStatus));
            }
        }
    }
    public string CommandTransmissionStatus => IsCommandTransmissionEnabled ? "Enabled (live DCS-BIOS commands)" : "Disabled (safe mode)";
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
        if (hardwareEvent.Kind == HardwareEventKind.Input && hardwareEvent.Input is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CaptureLearnedInput(hardwareEvent.Input);
                if (IsCommandTransmissionEnabled)
                {
                    _ = ProcessLiveMappingAsync(hardwareEvent.Input);
                }
            });
            return;
        }

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
                Devices.Add(new DeviceViewModel(
                    hardwareEvent.Device,
                    () => TestPanelOutputAsync(hardwareEvent.Device)));
                _ = PushPanelOutputAsync(hardwareEvent.Device.Type);
            }

            RaisePropertyChanged(nameof(DeviceSummary));
            RaisePropertyChanged(nameof(HasNoDevices));
        });
    }

    private async Task TestPanelOutputAsync(DeviceDescriptor device)
    {
        try
        {
            var output = device.Type switch
            {
                DeviceType.LogitechPz55 => new Output(
                    PanelOutputIds.Pz55GearLights,
                    new Pz55GearLights(Pz55GearLightColor.Green, Pz55GearLightColor.Yellow, Pz55GearLightColor.Red)),
                DeviceType.LogitechPz70 => new Output(
                    PanelOutputIds.Pz70Panel,
                    new Pz70PanelOutput(12345, -6789, Pz70AutopilotLights.All)),
                _ => throw new InvalidOperationException("This panel has no supported output test.")
            };

            await _hardwareService.SetOutputAsync(device.Id, output).ConfigureAwait(false);
            _activitySink.Publish(new ActivityEvent(
                DateTimeOffset.Now,
                ActivityCategory.Hardware,
                device.Type.ToString(),
                device.Type == DeviceType.LogitechPz55
                    ? "PZ55 gear LED test sent"
                    : "PZ70 display and LED test sent"));
        }
        catch (Exception exception)
        {
            _activitySink.Publish(new ActivityEvent(
                DateTimeOffset.Now,
                ActivityCategory.Error,
                device.Type.ToString(),
                $"Output test failed: {exception.Message}"));
        }
    }

    private void OnActivityPublished(object? sender, ActivityEvent activity)
    {
        if (Volatile.Read(ref _isLiveMonitorSessionActive) == 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (Volatile.Read(ref _isLiveMonitorSessionActive) == 0)
            {
                return;
            }

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
            _allControls.Clear();
            RefreshControlFilter();
            WritableControls.Clear();
            SelectedCommandControl = null;
            RefreshEditorCommands();
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
                WritableControls.Clear();
                SelectedCommandControl = null;
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
        SelectedMapping = null;
        Mappings.Clear();
        foreach (var device in profile.Profile.Devices)
        {
            foreach (var mapping in device.Mappings)
            {
                foreach (var action in mapping.Actions)
                {
                    Mappings.Add(new MappingViewModel(mapping, action));
                }
            }
        }
    }

    private void RefreshOutputBindings(ProfileViewModel profile)
    {
        SelectedOutputBinding = null;
        OutputBindings.Clear();
        foreach (var device in profile.Profile.Devices)
        foreach (var binding in device.OutputBindings ?? [])
            OutputBindings.Add(new OutputBindingViewModel(device.DeviceType, binding));
        RaisePropertyChanged(nameof(HasNoOutputBindings));
    }

    private bool CanAddOutputBinding() => !_isSavingProfile && SelectedProfile is not null &&
        SelectedOutputControl is not null && SelectedOutputTarget is not null &&
        !string.IsNullOrWhiteSpace(SelectedProfile.Profile.AircraftModule) &&
        string.Equals(SelectedProfile.Profile.AircraftModule, _dcsBiosClient.Aircraft, StringComparison.Ordinal);

    private async Task AddOutputBindingAsync()
    {
        if (!CanAddOutputBinding() || SelectedProfile is null || SelectedOutputControl is null || SelectedOutputTarget is null) return;
        var binding = new PanelOutputBinding(SelectedOutputTarget.Target, SelectedOutputControl.Identifier, SelectedOutputColor);
        var original = SelectedProfile;
        var updated = ProfileMappingEditor.UpsertOutput(original.Profile, binding);
        if (!await TrySaveProfileAsync(updated).ConfigureAwait(true)) return;
        ApplyUpdatedProfile(original, updated);
        ConfigureOutputSubscriptions();
    }

    private async Task RemoveSelectedOutputBindingAsync()
    {
        if (SelectedProfile is null || SelectedOutputBinding is null) return;
        var original = SelectedProfile;
        var updated = ProfileMappingEditor.RemoveOutput(original.Profile, SelectedOutputBinding.Binding);
        if (!await TrySaveProfileAsync(updated).ConfigureAwait(true)) return;
        ApplyUpdatedProfile(original, updated);
        ConfigureOutputSubscriptions();
    }

    private void ConfigureOutputSubscriptions()
    {
        var profile = SelectedProfile?.Profile;
        if (profile is null || _allControls.Count == 0) { _dcsBiosClient.SetOutputSubscriptions([]); return; }
        var subscriptions = profile.Devices.SelectMany(device => device.OutputBindings ?? [])
            .Select(binding => new { binding, control = _allControls.FirstOrDefault(control => string.Equals(control.Identifier, binding.ControlId, StringComparison.OrdinalIgnoreCase)) })
            .Where(item => item.control is not null)
            .Select(item => new DcsBiosOutputSubscription(item.binding.ControlId, item.control!.Metadata.Outputs[0]))
            .DistinctBy(subscription => subscription.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _dcsBiosClient.SetOutputSubscriptions(subscriptions);
    }

    private void ApplyOutputValue(DcsBiosValueChanged value)
    {
        var bindings = SelectedProfile?.Profile.Devices.SelectMany(device => device.OutputBindings ?? [])
            .Where(binding => string.Equals(binding.ControlId, value.ControlId, StringComparison.OrdinalIgnoreCase)).ToArray() ?? [];
        foreach (var binding in bindings)
        {
            if (binding.Target.ToString().StartsWith("Pz55", StringComparison.Ordinal))
            {
                var color = IsOutputActive(value.Value) ? binding.Color switch { PanelOutputColor.Red => Pz55GearLightColor.Red, PanelOutputColor.Yellow => Pz55GearLightColor.Yellow, _ => Pz55GearLightColor.Green } : Pz55GearLightColor.Off;
                _pz55Output = binding.Target switch { PanelOutputTarget.Pz55UpperGear => _pz55Output with { Upper = color }, PanelOutputTarget.Pz55LeftGear => _pz55Output with { Left = color }, _ => _pz55Output with { Right = color } };
                _ = PushPanelOutputAsync(DeviceType.LogitechPz55);
            }
            else
            {
                _pz70Output = binding.Target switch
                {
                    PanelOutputTarget.Pz70UpperDisplay => _pz70Output with { UpperDisplay = ToOutputNumber(value.Value) },
                    PanelOutputTarget.Pz70LowerDisplay => _pz70Output with { LowerDisplay = ToOutputNumber(value.Value) },
                    _ => _pz70Output with { Lights = SetPz70Light(_pz70Output.Lights, binding.Target, IsOutputActive(value.Value)) }
                };
                _ = PushPanelOutputAsync(DeviceType.LogitechPz70);
            }
        }
    }

    private async Task PushPanelOutputAsync(DeviceType type)
    {
        var output = type == DeviceType.LogitechPz55 ? new Output(PanelOutputIds.Pz55GearLights, _pz55Output) : new Output(PanelOutputIds.Pz70Panel, _pz70Output);
        foreach (var device in Devices.Where(device => device.Type == type))
        {
            try { await _hardwareService.SetOutputAsync(device.Id, output).ConfigureAwait(true); }
            catch (Exception) { }
        }
    }

    private void ResetPanelOutputs()
    {
        _pz55Output = new(Pz55GearLightColor.Off, Pz55GearLightColor.Off, Pz55GearLightColor.Off);
        _pz70Output = new(null, null);
    }

    private static bool IsOutputActive(object? value) => ToOutputNumber(value) is int number && number != 0;
    private static int? ToOutputNumber(object? value) => value switch { byte number => number, ushort number => number, int number => number, string text when int.TryParse(text, out var number) => number, _ => null };
    private static Pz70AutopilotLights SetPz70Light(Pz70AutopilotLights lights, PanelOutputTarget target, bool active)
    {
        var flag = target switch { PanelOutputTarget.Pz70ApLight => Pz70AutopilotLights.Ap, PanelOutputTarget.Pz70HdgLight => Pz70AutopilotLights.Hdg, PanelOutputTarget.Pz70NavLight => Pz70AutopilotLights.Nav, PanelOutputTarget.Pz70IasLight => Pz70AutopilotLights.Ias, PanelOutputTarget.Pz70AltLight => Pz70AutopilotLights.Alt, PanelOutputTarget.Pz70VsLight => Pz70AutopilotLights.Vs, PanelOutputTarget.Pz70AprLight => Pz70AutopilotLights.Apr, _ => Pz70AutopilotLights.Rev };
        return active ? lights | flag : lights & ~flag;
    }

    private void OnDcsBiosDataReceived(object? sender, DcsBiosDataReceived data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DcsBiosPacketCount = data.PacketNumber;
            LastDcsBiosData = $"{data.Timestamp.ToLocalTime():HH:mm:ss} - {data.ByteCount} bytes";
        });
    }

    private void OnDcsBiosValueChanged(object? sender, DcsBiosValueChanged value)
    {
        Dispatcher.UIThread.Post(() => ApplyOutputValue(value));
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
                OutputControls.Clear();
                foreach (var control in _allControls.Where(control => control.Metadata.Outputs.Count > 0)) OutputControls.Add(control);
                ConfigureOutputSubscriptions();
                RefreshWritableControlFilter();
                SelectedCommandControl = null;
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
            Dispatcher.UIThread.Post(() =>
            {
                if (!cancellationToken.IsCancellationRequested &&
                    string.Equals(_dcsBiosClient.Aircraft, aircraft, StringComparison.Ordinal))
                {
                    MetadataStatus = "Metadata loading failed";
                }
            });
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

    private void StartLearningInput()
    {
        IsLearningInput = !IsLearningInput;
        EditorStatus = IsLearningInput
            ? "Operate the PZ55 or PZ70 control you want to map"
            : "Input learning cancelled";
    }

    private void CaptureLearnedInput(InputEvent input)
    {
        if (!IsLearningInput)
        {
            return;
        }

        if (input.Kind == InputEventKind.Released || !input.IsActive)
        {
            return;
        }

        LearnedDeviceType = input.DeviceType.ToString();
        LearnedControlId = input.ControlId;
        TriggerOptions.Clear();

        if (input.Kind == InputEventKind.Clockwise)
        {
            TriggerOptions.Add(new MappingTriggerOption("Clockwise", PhysicalInputKind.EncoderClockwise, true));
        }
        else if (input.Kind == InputEventKind.CounterClockwise)
        {
            TriggerOptions.Add(new MappingTriggerOption("Counter-clockwise", PhysicalInputKind.EncoderCounterClockwise, true));
        }
        else
        {
            var kind = IsPersistentSwitch(input) ? PhysicalInputKind.Switch : PhysicalInputKind.Button;
            var activeLabel = kind == PhysicalInputKind.Switch ? "On" : "Pressed";
            var inactiveLabel = kind == PhysicalInputKind.Switch ? "Off" : "Released";
            TriggerOptions.Add(new MappingTriggerOption(activeLabel, kind, true));
            TriggerOptions.Add(new MappingTriggerOption(inactiveLabel, kind, false));
        }

        SelectedTriggerOption = TriggerOptions.FirstOrDefault(option => option.IsActive == input.IsActive)
                                ?? TriggerOptions.FirstOrDefault();
        IsLearningInput = false;
        EditorStatus = $"Learned {LearnedDeviceType} / {LearnedControlId}";
        RefreshEditorCommands();
    }

    private static bool IsPersistentSwitch(InputEvent input) =>
        input.DeviceType == DeviceType.LogitechPz55 ||
        input.DeviceType == DeviceType.LogitechPz70 &&
        (input.ControlId.StartsWith("KNOB_", StringComparison.OrdinalIgnoreCase) ||
         input.ControlId.Equals("AUTO_THROTTLE", StringComparison.OrdinalIgnoreCase));

    private async Task ProcessLiveMappingAsync(InputEvent input)
    {
        if (_dcsBiosClient.Status != ConnectionStatus.Connected || SelectedProfile is null)
        {
            return;
        }

        var kind = input.Kind switch
        {
            InputEventKind.Clockwise => PhysicalInputKind.EncoderClockwise,
            InputEventKind.CounterClockwise => PhysicalInputKind.EncoderCounterClockwise,
            _ when IsPersistentSwitch(input) => PhysicalInputKind.Switch,
            _ => PhysicalInputKind.Button
        };
        var matching = SelectedProfile.Profile.Devices
            .Where(device => string.Equals(device.DeviceType, input.DeviceType.ToString(), StringComparison.OrdinalIgnoreCase))
            .SelectMany(device => device.Mappings)
            .Where(mapping => string.Equals(mapping.ControlId, input.ControlId, StringComparison.OrdinalIgnoreCase) &&
                              mapping.Kind == kind &&
                              (mapping.IsActive is null || mapping.IsActive == input.IsActive))
            .SelectMany(mapping => mapping.Actions)
            .Where(action => action.Backend.Equals("DCS-BIOS", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var action in matching)
        {
            if (string.IsNullOrWhiteSpace(action.Argument)) continue;
            try
            {
                await _dcsBiosClient.SendCommandAsync(action.Command, action.Argument).ConfigureAwait(true);
                _activitySink.Publish(new ActivityEvent(DateTimeOffset.Now, ActivityCategory.Mapping, input.ControlId, $"Sent DCS-BIOS → {action.Command} {action.Argument}"));
            }
            catch (Exception exception)
            {
                _activitySink.Publish(new ActivityEvent(DateTimeOffset.Now, ActivityCategory.Error, "DCS-BIOS command", exception.Message));
            }
        }
    }

    private void SuggestMappingArgument()
    {
        if (SelectedCommandControl is not null && SelectedTriggerOption is not null)
        {
            MappingArgument = SelectedCommandControl.SuggestArgument(SelectedTriggerOption);
        }
    }

    private bool CanAddMapping() =>
        !_isSavingProfile && TryCreateDraftMapping(out _, updateStatus: false);

    private bool TryCreateDraftMapping(out InputMapping? mapping, bool updateStatus)
    {
        mapping = null;
        if (SelectedProfile is null ||
            string.IsNullOrWhiteSpace(SelectedProfile.Profile.AircraftModule) ||
            !string.Equals(SelectedProfile.Profile.AircraftModule, _dcsBiosClient.Aircraft, StringComparison.Ordinal))
        {
            if (updateStatus)
            {
                EditorStatus = "Start a mission and use the automatically selected aircraft profile";
            }
            return false;
        }

        if (LearnedDeviceType == "No input learned" || SelectedTriggerOption is null)
        {
            if (updateStatus)
            {
                EditorStatus = "Learn a physical panel input first";
            }
            return false;
        }

        if (SelectedCommandControl is null ||
            !_allControls.Contains(SelectedCommandControl) ||
            !string.Equals(ControlCatalogAircraft, _dcsBiosClient.Aircraft, StringComparison.Ordinal))
        {
            if (updateStatus)
            {
                EditorStatus = "Select a DCS-BIOS command";
            }
            return false;
        }

        var argument = MappingArgument.Trim();
        if (!SelectedCommandControl.IsValidArgument(argument))
        {
            if (updateStatus)
            {
                EditorStatus = "The argument is not valid for the selected DCS-BIOS interface";
            }
            return false;
        }

        mapping = new InputMapping(
            LearnedDeviceType,
            LearnedControlId,
            SelectedTriggerOption.Kind,
            [new ActionDefinition("DCS-BIOS", SelectedCommandControl.Identifier, argument)],
            SelectedTriggerOption.IsActive);
        return true;
    }

    private async Task AddMappingAsync()
    {
        if (!TryCreateDraftMapping(out var mapping, updateStatus: true) || mapping is null || SelectedProfile is null)
        {
            return;
        }

        var original = SelectedProfile;
        var updated = ProfileMappingEditor.Upsert(original.Profile, mapping);
        if (!await TrySaveProfileAsync(updated).ConfigureAwait(true))
        {
            return;
        }

        ApplyUpdatedProfile(original, updated);
        EditorStatus = $"Saved {mapping.ControlId} → {mapping.Actions[0].Command}";
        _activitySink.Publish(new ActivityEvent(
            DateTimeOffset.Now,
            ActivityCategory.Mapping,
            mapping.ControlId,
            $"Saved DCS-BIOS mapping to {mapping.Actions[0].Command}"));
    }

    private void TestMapping()
    {
        if (!TryCreateDraftMapping(out var mapping, updateStatus: true) || mapping is null)
        {
            return;
        }

        var action = mapping.Actions[0];
        EditorStatus = $"Safe test passed: {action.Command} {action.Argument} (not transmitted)";
        _activitySink.Publish(new ActivityEvent(
            DateTimeOffset.Now,
            ActivityCategory.Mapping,
            mapping.ControlId,
            $"Preview only: DCS-BIOS → {action.Command} {action.Argument}"));
    }

    private async Task RemoveSelectedMappingAsync()
    {
        if (SelectedProfile is null || SelectedMapping is null)
        {
            return;
        }

        var original = SelectedProfile;
        var removed = SelectedMapping.Mapping;
        var updated = ProfileMappingEditor.Remove(original.Profile, removed);
        if (!await TrySaveProfileAsync(updated).ConfigureAwait(true))
        {
            return;
        }

        ApplyUpdatedProfile(original, updated);
        EditorStatus = $"Removed mapping for {removed.ControlId}";
    }

    private async Task<bool> TrySaveProfileAsync(AircraftProfile profile)
    {
        _isSavingProfile = true;
        RefreshEditorCommands();
        try
        {
            var path = Path.Combine(ProfileDirectory, ProfileMappingEditor.GetFileName(profile));
            await _profileRepository.SaveAsync(profile, path).ConfigureAwait(true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            EditorStatus = $"Unable to save the profile: {exception.Message}";
            _activitySink.Publish(new ActivityEvent(
                DateTimeOffset.Now,
                ActivityCategory.Error,
                "Mapping editor",
                exception.Message));
            return false;
        }
        finally
        {
            _isSavingProfile = false;
            RefreshEditorCommands();
        }
    }

    private void ApplyUpdatedProfile(ProfileViewModel original, AircraftProfile profile)
    {
        var index = Profiles.IndexOf(original);
        if (index < 0)
        {
            return;
        }

        var wasSelected = ReferenceEquals(SelectedProfile, original);
        var updated = new ProfileViewModel(profile);
        Profiles[index] = updated;
        if (wasSelected)
        {
            SelectedProfile = updated;
        }
    }

    private void RefreshEditorCommands()
    {
        AddMappingCommand.RaiseCanExecuteChanged();
        TestMappingCommand.RaiseCanExecuteChanged();
        RemoveMappingCommand.RaiseCanExecuteChanged();
    }

    private void RefreshWritableControlFilter()
    {
        WritableControls.Clear();
        foreach (var control in _allControls.Where(control =>
                     control.CanReceiveCommands && control.Matches(MappingCommandSearchText)))
        {
            WritableControls.Add(control);
        }

        if (SelectedCommandControl is not null && !WritableControls.Contains(SelectedCommandControl))
        {
            SelectedCommandControl = null;
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
        _metadataLoadCancellation?.Cancel();
        _metadataLoadCancellation?.Dispose();
        _hardwareService.HardwareEventOccurred -= OnHardwareEvent;
        _activitySink.ActivityPublished -= OnActivityPublished;
        _dcsBiosClient.StateChanged -= OnDcsBiosStateChanged;
        _dcsBiosClient.DataReceived -= OnDcsBiosDataReceived;
    }
}
