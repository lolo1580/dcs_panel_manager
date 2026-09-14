namespace DCSPanel.Core.Mapping;

public enum PhysicalInputKind
{
    Switch,
    Button,
    EncoderClockwise,
    EncoderCounterClockwise,
    Axis
}

public sealed record PhysicalInput(
    string DeviceType,
    string ControlId,
    PhysicalInputKind Kind,
    bool IsActive);

public sealed record ActionDefinition(
    string Backend,
    string Command,
    string? Argument = null);

public sealed record InputMapping(
    string DeviceType,
    string ControlId,
    PhysicalInputKind Kind,
    IReadOnlyList<ActionDefinition> Actions);

public interface IActionExecutor
{
    string Backend { get; }

    ValueTask ExecuteAsync(ActionDefinition action, CancellationToken cancellationToken);
}
