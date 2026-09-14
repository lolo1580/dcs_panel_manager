using DCSPanel.Core.Events;
using System.Globalization;

namespace DCSPanel.App.ViewModels;

public sealed class ActivityEventViewModel(ActivityEvent activity)
{
    public ActivityCategory CategoryValue { get; } = activity.Category;
    public string Time { get; } = activity.Timestamp.LocalDateTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    public string Category { get; } = activity.Category switch
    {
        ActivityCategory.DcsBios => "DCS-BIOS",
        _ => activity.Category.ToString()
    };
    public string Source { get; } = activity.Source;
    public string Message { get; } = activity.Message;
}
