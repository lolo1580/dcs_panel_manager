namespace DCSPanel.Core.Events;

public sealed class ActivityHub : IActivitySink
{
    public event EventHandler<ActivityEvent>? ActivityPublished;

    public void Publish(ActivityEvent activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ActivityPublished?.Invoke(this, activity);
    }
}
