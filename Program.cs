using Avalonia;
using DicresPhotosUploader.Config;
using DicresPhotosUploader.Google;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.State;

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
    await using var gate = SingleRunGuard.TryAcquire();
    if (gate is null)
    {
        // a run is already in progress (manual or scheduled): exit without doing anything.
        return 0;
    }

    var config = LoadConfiguration();

    await using var logWriter = CreateRunLogWriter();
    var progress = CreateLogProgress(logWriter);

    var startedUtc = DateTime.UtcNow;
    var summary = await ExecuteUploadAsync(config, progress);
    AppendRunHistory(config, summary, startedUtc);

    return summary.Success ? 0 : 1;
}

static AppConfig LoadConfiguration()
{
    Directory.CreateDirectory(AppConfig.AppDataFolder);

    var config = new ConfigStore().Load();
    Loc.Initialize(config.LanguagePreference);

    return config;
}

static StreamWriter CreateRunLogWriter()
{
    var logsDir = Path.Combine(AppConfig.AppDataFolder, "logs");
    Directory.CreateDirectory(logsDir);

    var logPath = Path.Combine(logsDir, $"run-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    return new StreamWriter(logPath, append: false) { AutoFlush = true };
}

static IProgress<string> CreateLogProgress(TextWriter logWriter) =>
    new Progress<string>(line => logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}"));

static async Task<UploadRunSummary> ExecuteUploadAsync(AppConfig config, IProgress<string> progress)
{
    var stateStore = new StateStore(config.StateFilePath);
    var state = stateStore.Load();

    return await new UploadService().RunAsync(config, stateStore, state, progress, CancellationToken.None);
}

static void AppendRunHistory(AppConfig config, UploadRunSummary summary, DateTime startedUtc)
{
    var historyStore = new RunHistoryStore(config.RunHistoryFilePath);

    historyStore.Append(new RunHistoryEntry
    {
        StartedUtc = startedUtc,
        FinishedUtc = DateTime.UtcNow,
        Origin = RunOrigin.Scheduled,
        Status = ResolveRunStatus(summary),
        UploadedThisRun = summary.UploadedThisRun,
        UploadedFilesTotal = summary.UploadedFilesTotal,
        SkippedFilesTotal = summary.SkippedFilesTotal,
        ErrorMessage = summary.ErrorMessage
    });
}

static RunStatus ResolveRunStatus(UploadRunSummary summary)
{
    if (summary.QuotaExceeded)
    {
        return RunStatus.QuotaExceeded;
    }

    return summary.Success ? RunStatus.Ok : RunStatus.Error;
}

