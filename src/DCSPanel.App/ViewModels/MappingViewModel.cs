using DCSPanel.Core.Mapping;

namespace DCSPanel.App.ViewModels;

public sealed class MappingViewModel(
    string deviceType,
    string controlId,
    PhysicalInputKind inputKind,
    ActionDefinition action)
{
    public string DeviceType { get; } = deviceType;
    public string ControlId { get; } = controlId;
    public string InputKind { get; } = inputKind.ToString();
    public string Backend { get; } = action.Backend;
    public string Command { get; } = action.Command;
    public string Argument { get; } = action.Argument ?? "-";
}
