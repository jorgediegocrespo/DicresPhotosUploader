using Avalonia;
using DicresPhotosUploader.Config;
using DicresPhotosUploader.Google;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.State;
using DicresPhotosUploader.UI.ViewModels;

if (args.Contains("--run-scheduled"))
{
    return await RunHeadlessAsync();
}

BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
return 0;

static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<DicresPhotosUploader.App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();

// Mode invoked by Windows Task Scheduler / macOS launchd: no window, runs and exits.
static async Task<int> RunHeadlessAsync()
{
    using var gate = SingleRunGuard.TryAcquire();
    if (gate is null)
    {
        return 0; // a run is already in progress (manual or scheduled): exit without doing anything.
    }

    Directory.CreateDirectory(AppConfig.AppDataFolder);

    var configStore = new ConfigStore();
    var config = configStore.Load();

    Loc.Initialize(config.LanguagePreference);

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

