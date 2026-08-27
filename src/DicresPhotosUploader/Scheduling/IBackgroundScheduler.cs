namespace DicresPhotosUploader.Scheduling;

public interface IBackgroundScheduler
{
    Task RegisterAsync(IReadOnlyList<ScheduleEntry> entries, string executablePath);

    Task UnregisterAsync();

    Task<bool> IsRegisteredAsync();

    static IBackgroundScheduler Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsTaskSchedulerRegistrar();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacLaunchdRegistrar();
        }

        throw new PlatformNotSupportedException("Scheduled execution is only implemented for Windows and macOS.");
    }
}
