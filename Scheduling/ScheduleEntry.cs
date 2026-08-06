namespace GooglePhotosUploader.Scheduling;

/// <summary>A recurring weekly trigger: every <see cref="DayOfWeek"/> at <see cref="Hour"/>:<see cref="Minute"/>.</summary>
public class ScheduleEntry
{
    public DayOfWeek DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
}
