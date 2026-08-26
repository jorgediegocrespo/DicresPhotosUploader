namespace DicresPhotosUploader.Scheduling;

public static class ScheduleCalculator
{
    public static DateTime? GetNextOccurrence(IReadOnlyList<ScheduleEntry> entries, DateTime? fromLocal = null)
    {
        if (entries.Count == 0)
        {
            return null;
        }

        return entries.Select(e => GetNextOccurrence(e, fromLocal)).Min();
    }

    public static DateTime GetNextOccurrence(ScheduleEntry entry, DateTime? fromLocal = null)
    {
        var now = fromLocal ?? DateTime.Now;
        var candidate = new DateTime(now.Year, now.Month, now.Day, entry.Hour, entry.Minute, 0);

        var daysUntil = ((int)entry.DayOfWeek - (int)now.DayOfWeek + 7) % 7;
        candidate = candidate.AddDays(daysUntil);

        if (candidate <= now)
        {
            candidate = candidate.AddDays(7);
        }

        return candidate;
    }
}
