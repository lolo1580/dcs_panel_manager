using System.Collections.ObjectModel;
using Avalonia.Threading;
using DCSPanel.Core.Events;
using DCSPanel.Hardware.Abstractions;
using DCSPanel.Hardware.Models;

namespace DCSPanel.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaximumActivityCount = 500;
    private readonly IHardwareService _hardwareService;
    private readonly IActivitySink _activitySink;
    private readonly List<ActivityEventViewModel> _allActivities = [];
    private bool _showHardware = true;
    private bool _showMapping = true;
    private bool _showDcsBios = true;
    private bool _showErrors = true;

    public MainWindowViewModel(IHardwareService hardwareService, IActivitySink activitySink)
    {
        _hardwareService = hardwareService;
        _activitySink = activitySink;
        _hardwareService.HardwareEventOccurred += OnHardwareEvent;
        _activitySink.ActivityPublished += OnActivityPublished;
        DcsWorldStatus = "Disconnected";
        DcsBiosStatus = "Disconnected";
        DetectedAircraft = "-";
        ActiveProfile = "Generic";
    }

    public ObservableCollection<DeviceViewModel> Devices { get; } = [];
    public ObservableCollection<ActivityEventViewModel> Activities { get; } = [];

    public string DcsWorldStatus { get; }
    public string DcsBiosStatus { get; }
    public string DetectedAircraft { get; }
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
    }
}
