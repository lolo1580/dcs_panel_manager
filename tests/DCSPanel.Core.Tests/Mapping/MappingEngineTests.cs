using DCSPanel.Core.Events;
using DCSPanel.Core.Mapping;

namespace DCSPanel.Core.Tests.Mapping;

public sealed class MappingEngineTests
{
    [Fact]
    public async Task ProcessAsyncExecutesMatchingBackendOnly()
    {
        var executor = new RecordingExecutor("DCS-BIOS");
        var engine = new MappingEngine([executor], new ActivityHub());
        var mappings = new[]
        {
            new InputMapping("LogitechPz55", "MASTER_BAT", PhysicalInputKind.Switch,
                [new ActionDefinition("DCS-BIOS", "MAIN_PWR_SW", "BATT")]),
            new InputMapping("LogitechPz55", "FUEL_PUMP", PhysicalInputKind.Switch,
                [new ActionDefinition("DCS-BIOS", "FUEL_PUMP", "1")])
        };

        await engine.ProcessAsync(
            new PhysicalInput("LogitechPz55", "MASTER_BAT", PhysicalInputKind.Switch, true),
            mappings);

        var action = Assert.Single(executor.Actions);
        Assert.Equal("MAIN_PWR_SW", action.Command);
        Assert.Equal("BATT", action.Argument);
    }

    private sealed class RecordingExecutor(string backend) : IActionExecutor
    {
        public string Backend { get; } = backend;
        public List<ActionDefinition> Actions { get; } = [];

        public ValueTask ExecuteAsync(ActionDefinition action, CancellationToken cancellationToken)
        {
            Actions.Add(action);
            return ValueTask.CompletedTask;
        }
    }
}
