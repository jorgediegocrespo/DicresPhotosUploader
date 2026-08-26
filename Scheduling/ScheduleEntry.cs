namespace DicresPhotosUploader.Scheduling;

public class ScheduleEntry
{
    public DayOfWeek DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
}
