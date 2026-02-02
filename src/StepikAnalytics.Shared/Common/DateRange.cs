namespace StepikAnalytics.Shared.Common;

public enum MetricsRange
{
    Day,
    Week,
    Month,
    Year
}

public static class DateRangeHelper
{
    public static (DateTime Start, DateTime End) GetRange(MetricsRange range, DateTime anchorDate)
    {
        var end = anchorDate.Date.AddDays(1).AddTicks(-1);
        var start = range switch
        {
            MetricsRange.Day => anchorDate.Date,
            MetricsRange.Week => anchorDate.Date.AddDays(-6),
            MetricsRange.Month => anchorDate.Date.AddDays(-29),
            MetricsRange.Year => anchorDate.Date.AddDays(-364),
            _ => anchorDate.Date
        };
        return (start, end);
    }

    public static (DateTime Start, DateTime End) GetPreviousRange(MetricsRange range, DateTime anchorDate)
    {
        var (currentStart, _) = GetRange(range, anchorDate);
        var duration = range switch
        {
            MetricsRange.Day => TimeSpan.FromDays(1),
            MetricsRange.Week => TimeSpan.FromDays(7),
            MetricsRange.Month => TimeSpan.FromDays(30),
            MetricsRange.Year => TimeSpan.FromDays(365),
            _ => TimeSpan.FromDays(1)
        };
        var prevEnd = currentStart.AddTicks(-1);
        var prevStart = currentStart.Add(-duration);
        return (prevStart, prevEnd);
    }
}
