using DCSPanel.Core.Mapping;

namespace DCSPanel.App.ViewModels;

public sealed class MappingViewModel(InputMapping mapping, ActionDefinition action)
{
    public InputMapping Mapping { get; } = mapping;
    public string DeviceType { get; } = mapping.DeviceType;
    public string ControlId { get; } = mapping.ControlId;
    public string InputKind { get; } = mapping.Kind.ToString();
    public string Trigger { get; } = mapping.IsActive switch
    {
        true => "On / Pressed",
        false => "Off / Released",
        null => "Any state"
    };
    public string Backend { get; } = action.Backend;
    public string Command { get; } = action.Command;
    public string Argument { get; } = action.Argument ?? "-";
}
