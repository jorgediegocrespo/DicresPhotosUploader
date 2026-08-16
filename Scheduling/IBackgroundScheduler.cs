namespace DicresPhotosUploader.Scheduling;

/// <summary>Registers/removes the periodic headless run in the OS's native scheduler.</summary>
public interface IBackgroundScheduler
{
    Task RegisterAsync(IReadOnlyList<ScheduleEntry> entries, string executablePath);

    Task UnregisterAsync();

    /// <summary>True if the task/agent is still registered in the OS.</summary>
    Task<bool> IsRegisteredAsync();

    /// <summary>Creates the appropriate registrar for the current OS.</summary>
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
