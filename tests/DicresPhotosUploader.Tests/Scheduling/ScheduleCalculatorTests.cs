using DicresPhotosUploader.Scheduling;

namespace DicresPhotosUploader.Tests.Scheduling;

public class ScheduleCalculatorTests
{
    private static readonly DateTime Monday1030 = new(2025, 1, 6, 10, 30, 0);

    [Fact]
    public void GetNextOccurrence_EmptyList_ReturnsNull()
    {
        var result = ScheduleCalculator.GetNextOccurrence(Array.Empty<ScheduleEntry>(), Monday1030);
        Assert.Null(result);
    }

    [Fact]
    public void GetNextOccurrence_SameDayLaterTime_ReturnsTodayAtThatTime()
    {
        var entry = new ScheduleEntry { DayOfWeek = DayOfWeek.Monday, Hour = 15, Minute = 0 };
        var result = ScheduleCalculator.GetNextOccurrence(entry, Monday1030);

        Assert.Equal(new DateTime(2025, 1, 6, 15, 0, 0), result);
    }

    [Fact]
    public void GetNextOccurrence_SameDaySameTime_ReturnsNextWeek()
    {
        // Exact same moment counts as "not in the future", so it should roll to next week.
        var entry = new ScheduleEntry { DayOfWeek = DayOfWeek.Monday, Hour = 10, Minute = 30 };
        var result = ScheduleCalculator.GetNextOccurrence(entry, Monday1030);

        Assert.Equal(new DateTime(2025, 1, 13, 10, 30, 0), result);
    }

    [Fact]
    public void GetNextOccurrence_SameDayEarlierTime_ReturnsNextWeek()
    {
        var entry = new ScheduleEntry { DayOfWeek = DayOfWeek.Monday, Hour = 9, Minute = 0 };
        var result = ScheduleCalculator.GetNextOccurrence(entry, Monday1030);

        Assert.Equal(new DateTime(2025, 1, 13, 9, 0, 0), result);
    }

    [Fact]
    public void GetNextOccurrence_DifferentDayLaterInWeek_ReturnsThatDay()
    {
        var entry = new ScheduleEntry { DayOfWeek = DayOfWeek.Wednesday, Hour = 8, Minute = 0 };
        var result = ScheduleCalculator.GetNextOccurrence(entry, Monday1030);

        Assert.Equal(new DateTime(2025, 1, 8, 8, 0, 0), result);
    }

    [Fact]
    public void GetNextOccurrence_DifferentDayEarlierInWeek_ReturnsNextWeek()
    {
        // Sunday is earlier in the week than Monday.
        var entry = new ScheduleEntry { DayOfWeek = DayOfWeek.Sunday, Hour = 8, Minute = 0 };
        var result = ScheduleCalculator.GetNextOccurrence(entry, Monday1030);

        Assert.Equal(new DateTime(2025, 1, 12, 8, 0, 0), result);
    }

    [Fact]
    public void GetNextOccurrenceList_MultipleEntries_ReturnsEarliest()
    {
        var entries = new List<ScheduleEntry>
        {
            new() { DayOfWeek = DayOfWeek.Wednesday, Hour = 8, Minute = 0 },   // Jan 8
            new() { DayOfWeek = DayOfWeek.Friday,    Hour = 8, Minute = 0 },   // Jan 10
            new() { DayOfWeek = DayOfWeek.Tuesday,   Hour = 8, Minute = 0 },   // Jan 7
        };

        var result = ScheduleCalculator.GetNextOccurrence(entries, Monday1030);

        Assert.Equal(new DateTime(2025, 1, 7, 8, 0, 0), result);
    }

    [Fact]
    public void GetNextOccurrenceList_SingleEntry_MatchesSingleOverload()
    {
        var entry = new ScheduleEntry { DayOfWeek = DayOfWeek.Thursday, Hour = 20, Minute = 15 };
        var list = new List<ScheduleEntry> { entry };

        var fromList = ScheduleCalculator.GetNextOccurrence(list, Monday1030);
        var fromSingle = ScheduleCalculator.GetNextOccurrence(entry, Monday1030);

        Assert.Equal(fromSingle, fromList);
    }
}
