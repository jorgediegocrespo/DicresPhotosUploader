using System.Runtime.Versioning;
using Microsoft.Win32.TaskScheduler;
using Task = System.Threading.Tasks.Task;

namespace DicresPhotosUploader.Scheduling;

[SupportedOSPlatform("windows")]
public class WindowsTaskSchedulerRegistrar : IBackgroundScheduler
{
    private const string TaskName = "DicresPhotosUploader-Scheduled";

    public Task RegisterAsync(IReadOnlyList<ScheduleEntry> entries, string executablePath)
    {
        using var ts = new TaskService();
        var td = ts.NewTask();
        td.RegistrationInfo.Description = "Scheduled upload of photos/videos to Google Photos.";

        foreach (var entry in entries)
        {
            var trigger = new WeeklyTrigger(DayOfWeekToDaysOfTheWeek(entry.DayOfWeek))
            {
                StartBoundary = ScheduleCalculator.GetNextOccurrence(entry)
            };
            td.Triggers.Add(trigger);
        }

        // Runs as soon as possible if the computer was off at the scheduled time.
        td.Settings.StartWhenAvailable = true;
        td.Settings.DisallowStartIfOnBatteries = false;
        td.Settings.StopIfGoingOnBatteries = false;
        td.Settings.ExecutionTimeLimit = TimeSpan.Zero; // no limit: large uploads can take a while

        td.Actions.Add(new ExecAction(executablePath, "--run-scheduled"));

        // "Only when the user is logged on": does not require storing credentials.
        ts.RootFolder.RegisterTaskDefinition(TaskName, td, TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);

        return Task.CompletedTask;
    }

    public Task UnregisterAsync()
    {
        using var ts = new TaskService();
        ts.RootFolder.DeleteTask(TaskName, exceptionOnNotExists: false);
        return Task.CompletedTask;
    }

    public Task<bool> IsRegisteredAsync()
    {
        using var ts = new TaskService();
        return Task.FromResult(ts.GetTask(TaskName) is not null);
    }

    private static DaysOfTheWeek DayOfWeekToDaysOfTheWeek(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => DaysOfTheWeek.Sunday,
        DayOfWeek.Monday => DaysOfTheWeek.Monday,
        DayOfWeek.Tuesday => DaysOfTheWeek.Tuesday,
        DayOfWeek.Wednesday => DaysOfTheWeek.Wednesday,
        DayOfWeek.Thursday => DaysOfTheWeek.Thursday,
        DayOfWeek.Friday => DaysOfTheWeek.Friday,
        DayOfWeek.Saturday => DaysOfTheWeek.Saturday,
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };
}
