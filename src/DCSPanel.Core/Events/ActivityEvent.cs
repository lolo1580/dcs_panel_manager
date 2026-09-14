namespace DCSPanel.Core.Events;

public enum ActivityCategory
{
    Hardware,
    Mapping,
    DcsBios,
    Error
}

public sealed record ActivityEvent(
    DateTimeOffset Timestamp,
    ActivityCategory Category,
    string Source,
    string Message);
