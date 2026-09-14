namespace DCSPanel.Core.Events;

public interface IActivitySink
{
    event EventHandler<ActivityEvent>? ActivityPublished;

    void Publish(ActivityEvent activity);
}
