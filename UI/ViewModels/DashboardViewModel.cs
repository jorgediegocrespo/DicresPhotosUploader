using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GooglePhotosUploader.Config;
using GooglePhotosUploader.Google;
using GooglePhotosUploader.State;

namespace GooglePhotosUploader.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    /// <summary>Same name used by headless mode to avoid overlapping with a scheduled run.</summary>
    public const string SingleRunMutexName = "GooglePhotosUploader-SingleRun";

    private readonly AppConfig _config;
    private readonly StateStore _stateStore;
    private readonly RunHistoryStore _historyStore;
    private readonly UploadService _uploadService = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _lastRunSummary = "No upload has been run yet.";

    [ObservableProperty]
    private int _uploadedFilesTotal;

    public ObservableCollection<AlbumProgress> Albums { get; } = new();

    public ObservableCollection<string> LogLines { get; } = new();

    public DashboardViewModel(AppConfig config, StateStore stateStore, RunHistoryStore historyStore)
    {
        _config = config;
        _stateStore = stateStore;
        _historyStore = historyStore;

        RefreshAlbums();
    }

    [RelayCommand(CanExecute = nameof(CanRunNow))]
    private async Task RunNowAsync()
    {
        using var mutex = new Mutex(initiallyOwned: false, SingleRunMutexName);
        if (!mutex.WaitOne(TimeSpan.Zero))
        {
            LogLines.Add("A run is already in progress (manual or scheduled). Try again in a few minutes.");
            return;
        }

        try
        {
            IsRunning = true;
            LogLines.Clear();

            var progress = new Progress<string>(line => LogLines.Add(line));
            var state = _stateStore.Load();
            var startedUtc = DateTime.UtcNow;

            var summary = await _uploadService.RunAsync(_config, _stateStore, state, progress, CancellationToken.None);

            _historyStore.Append(new RunHistoryEntry
            {
                StartedUtc = startedUtc,
                FinishedUtc = DateTime.UtcNow,
                Origin = RunOrigin.Manual,
                Status = summary.QuotaExceeded ? RunStatus.QuotaExceeded : (summary.Success ? RunStatus.Ok : RunStatus.Error),
                UploadedThisRun = summary.UploadedThisRun,
                UploadedFilesTotal = summary.UploadedFilesTotal,
                SkippedFilesTotal = summary.SkippedFilesTotal,
                ErrorMessage = summary.ErrorMessage
            });

            LastRunSummary = summary.Success
                ? $"Last run: {summary.UploadedThisRun} uploaded, {summary.SkippedFilesTotal} discarded (historical: {summary.UploadedFilesTotal})."
                : $"Last run had errors: {summary.ErrorMessage}";

            RefreshAlbums();
        }
        finally
        {
            IsRunning = false;
            mutex.ReleaseMutex();
        }
    }

    private bool CanRunNow() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRunNow))]
    private async Task ReprocessErrorsAsync()
    {
        using var mutex = new Mutex(initiallyOwned: false, SingleRunMutexName);
        if (!mutex.WaitOne(TimeSpan.Zero))
        {
            LogLines.Add("A run is already in progress (manual or scheduled). Try again in a few minutes.");
            return;
        }

        try
        {
            IsRunning = true;
            LogLines.Clear();

            var progress = new Progress<string>(line => LogLines.Add(line));
            var state = _stateStore.Load();
            var startedUtc = DateTime.UtcNow;

            var summary = await _uploadService.ReprocessErroredAsync(_config, _stateStore, state, progress, CancellationToken.None);

            _historyStore.Append(new RunHistoryEntry
            {
                StartedUtc = startedUtc,
                FinishedUtc = DateTime.UtcNow,
                Origin = RunOrigin.Manual,
                Status = summary.QuotaExceeded ? RunStatus.QuotaExceeded : (summary.Success ? RunStatus.Ok : RunStatus.Error),
                UploadedThisRun = summary.UploadedThisRun,
                UploadedFilesTotal = summary.UploadedFilesTotal,
                SkippedFilesTotal = summary.SkippedFilesTotal,
                ErrorMessage = summary.ErrorMessage
            });

            LastRunSummary = summary.Success
                ? $"Last reprocess run: {summary.UploadedThisRun} re-uploaded (historical: {summary.UploadedFilesTotal})."
                : $"Last reprocess run had errors: {summary.ErrorMessage}";

            RefreshAlbums();
        }
        finally
        {
            IsRunning = false;
            mutex.ReleaseMutex();
        }
    }

    partial void OnIsRunningChanged(bool value)
    {
        RunNowCommand.NotifyCanExecuteChanged();
        ReprocessErrorsCommand.NotifyCanExecuteChanged();
    }

    private void RefreshAlbums()
    {
        Albums.Clear();

        if (!Directory.Exists(_config.RootFolder))
        {
            return;
        }

        var state = _stateStore.Load();
        UploadedFilesTotal = state.UploadedFiles.Count;

        foreach (var folder in Directory.GetDirectories(_config.RootFolder).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var albumName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar));

            var files = Directory.GetFiles(folder)
                .Where(f => _config.AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .ToList();

            var uploaded = files.Count(f => state.UploadedFiles.ContainsKey(f));

            Albums.Add(new AlbumProgress { Name = albumName, UploadedCount = uploaded, TotalCount = files.Count });
        }
    }
}
