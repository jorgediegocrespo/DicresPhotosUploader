namespace DicresPhotosUploader.Config;

/// <summary>
/// Cross-process mutual exclusion between a manual run, "Reprocess errors", and the
/// headless scheduled run (each can be a different process). Backed by an exclusive
/// file lock rather than a named Mutex/Semaphore: a <see cref="FileStream"/> lock has
/// no thread affinity, so it can safely be acquired on one thread and released (via
/// Dispose) on another, which is required since release happens after awaits whose
/// continuation may resume on a different thread pool thread. Named Semaphore isn't
/// even supported cross-process on macOS/Unix, and a named Mutex is thread-affine.
/// </summary>
public static class SingleRunGuard
{
    private static string LockFilePath => Path.Combine(AppConfig.AppDataFolder, "run.lock");

    /// <summary>Returns the held lock, or null if another process already holds it.</summary>
    public static FileStream? TryAcquire()
    {
        Directory.CreateDirectory(AppConfig.AppDataFolder);

        try
        {
            return new FileStream(LockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
