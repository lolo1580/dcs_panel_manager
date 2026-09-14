using DCSPanel.Core.Events;

namespace DCSPanel.Core.Mapping;

public sealed class MappingEngine(
    IEnumerable<IActionExecutor> executors,
    IActivitySink activitySink)
{
    private readonly Dictionary<string, IActionExecutor> _executors = executors
        .ToDictionary(executor => executor.Backend, StringComparer.OrdinalIgnoreCase);

    public async ValueTask ProcessAsync(
        PhysicalInput input,
        IEnumerable<InputMapping> mappings,
        CancellationToken cancellationToken = default)
    {
        foreach (var mapping in mappings.Where(mapping => Matches(mapping, input)))
        {
            foreach (var action in mapping.Actions)
            {
                if (!_executors.TryGetValue(action.Backend, out var executor))
                {
                    activitySink.Publish(new ActivityEvent(
                        DateTimeOffset.Now,
                        ActivityCategory.Error,
                        nameof(MappingEngine),
                        $"Backend inconnu : {action.Backend}"));
                    continue;
                }

                activitySink.Publish(new ActivityEvent(
                    DateTimeOffset.Now,
                    ActivityCategory.Mapping,
                    input.ControlId,
                    $"{action.Backend} → {action.Command}"));

                await executor.ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool Matches(InputMapping mapping, PhysicalInput input) =>
        string.Equals(mapping.DeviceType, input.DeviceType, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(mapping.ControlId, input.ControlId, StringComparison.OrdinalIgnoreCase) &&
        mapping.Kind == input.Kind;
}
