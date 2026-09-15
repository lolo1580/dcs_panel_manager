using DCSPanel.Profiles.Models;

namespace DCSPanel.App.ViewModels;

public sealed class OutputBindingViewModel(string deviceType, PanelOutputBinding binding)
{
    public string DeviceType { get; } = deviceType;
    public PanelOutputBinding Binding { get; } = binding;
    public string Target { get; } = binding.Target.ToString();
    public string ControlId { get; } = binding.ControlId;
    public string Color { get; } = binding.Color.ToString();
}

public sealed record OutputTargetOption(PanelOutputTarget Target, string Label);
