using Avalonia;
using GooglePhotosUploader.Config;
using GooglePhotosUploader.Google;
using GooglePhotosUploader.State;
using GooglePhotosUploader.UI.ViewModels;

if (args.Contains("--run-scheduled"))
{
    return await RunHeadlessAsync();
}

BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
return 0;

static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<GooglePhotosUploader.App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();

// Mode invoked by Windows Task Scheduler / macOS launchd: no window, runs and exits.
static async Task<int> RunHeadlessAsync()
{
    using var mutex = new Mutex(initiallyOwned: false, DashboardViewModel.SingleRunMutexName);
    if (!mutex.WaitOne(TimeSpan.Zero))
    {
        return 0; // a run is already in progress (manual or scheduled): exit without doing anything.
    }

    try
    {
        Directory.CreateDirectory(AppConfig.AppDataFolder);

        var configStore = new ConfigStore();
        var config = configStore.Load();

        var logsDir = Path.Combine(AppConfig.AppDataFolder, "logs");
        Directory.CreateDirectory(logsDir);
        var logPath = Path.Combine(logsDir, $"run-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        await using var logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
        var progress = new Progress<string>(line => logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}"));

        var stateStore = new StateStore(config.StateFilePath);
        var state = stateStore.Load();
        var historyStore = new RunHistoryStore(config.RunHistoryFilePath);

        var startedUtc = DateTime.UtcNow;
        var summary = await new UploadService().RunAsync(config, stateStore, state, progress, CancellationToken.None);

        historyStore.Append(new RunHistoryEntry
        {
            StartedUtc = startedUtc,
            FinishedUtc = DateTime.UtcNow,
            Origin = RunOrigin.Scheduled,
            Status = summary.QuotaExceeded ? RunStatus.QuotaExceeded : (summary.Success ? RunStatus.Ok : RunStatus.Error),
            UploadedThisRun = summary.UploadedThisRun,
            UploadedFilesTotal = summary.UploadedFilesTotal,
            SkippedFilesTotal = summary.SkippedFilesTotal,
            ErrorMessage = summary.ErrorMessage
        });

        return summary.Success ? 0 : 1;
    }
    finally
    {
        mutex.ReleaseMutex();
    }
}

