using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DicresPhotosUploader.Config;
using DicresPhotosUploader.Google;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.State;

namespace DicresPhotosUploader.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly AppConfig _config;
    private readonly StateStore _stateStore;
    private readonly RunHistoryStore _historyStore;
    private readonly UploadService _uploadService = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _lastRunSummary = Loc.Get("Dashboard_NoRunYet");

    [ObservableProperty]
    private int _uploadedFilesTotal;

    [ObservableProperty]
    private string _historicalTotalText = "";

    private ObservableCollection<AlbumProgress> Albums { get; } = new();

    public ObservableCollection<AlbumProgress> FilteredAlbums { get; } = new();

    [ObservableProperty]
    private bool _showOnlyAlbumsWithErrors;

    public ObservableCollection<string> LogLines { get; } = new();

    public DashboardViewModel(AppConfig config, StateStore stateStore, RunHistoryStore historyStore)
    {
        _config = config;
        _stateStore = stateStore;
        _historyStore = historyStore;

        RefreshAlbums();
    }

    partial void OnUploadedFilesTotalChanged(int value)
    {
        HistoricalTotalText = Loc.Format("Dashboard_HistoricalTotal", value);
    }

    partial void OnShowOnlyAlbumsWithErrorsChanged(bool value) => ApplyAlbumFilter();

    private void ApplyAlbumFilter()
    {
        FilteredAlbums.Clear();
        foreach (var album in Albums.Where(a => !ShowOnlyAlbumsWithErrors || a.HasError))
        {
            FilteredAlbums.Add(album);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunNow))]
    private async Task RunNowAsync()
    {
        await using var gate = SingleRunGuard.TryAcquire();
        if (gate is null)
        {
            LogLines.Add(Loc.Get("Dashboard_RunInProgress"));
            return;
        }

        try
        {
            IsRunning = true;
            LogLines.Clear();
            RefreshAlbums();

            var (progress, albumProgress) = CreateProgressReporters();
            var state = _stateStore.Load();
            var startedUtc = DateTime.UtcNow;

            var summary = await _uploadService.RunAsync(_config, _stateStore, state, progress, CancellationToken.None, albumProgress);

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
                ? Loc.Format("Dashboard_LastRunSuccess", summary.UploadedThisRun, summary.SkippedFilesTotal, summary.UploadedFilesTotal)
                : Loc.Format("Dashboard_LastRunError", summary.ErrorMessage);

            RefreshAlbums();
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool CanRunNow() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRunNow))]
    private async Task ReprocessErrorsAsync()
    {
        using var gate = SingleRunGuard.TryAcquire();
        if (gate is null)
        {
            LogLines.Add(Loc.Get("Dashboard_RunInProgress"));
            return;
        }

        try
        {
            IsRunning = true;
            LogLines.Clear();
            RefreshAlbums();

            var (progress, albumProgress) = CreateProgressReporters();
            var state = _stateStore.Load();
            var startedUtc = DateTime.UtcNow;

            var summary = await _uploadService.ReprocessErroredAsync(_config, _stateStore, state, progress, CancellationToken.None, albumProgress);

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
                ? Loc.Format("Dashboard_LastReprocessSuccess", summary.UploadedThisRun, summary.UploadedFilesTotal)
                : Loc.Format("Dashboard_LastReprocessError", summary.ErrorMessage);

            RefreshAlbums();
        }
        finally
        {
            IsRunning = false;
        }
    }

    partial void OnIsRunningChanged(bool value)
    {
        RunNowCommand.NotifyCanExecuteChanged();
        ReprocessErrorsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Builds the log/album progress channels used by a run. Both explicitly marshal
    /// back to the UI thread via the Avalonia dispatcher so collection/property changes
    /// are always applied where the UI can observe them live, regardless of which
    /// thread the upload's async continuations happen to resume on.
    /// </summary>
    private (IProgress<string> Log, IProgress<AlbumUploadProgress> Album) CreateProgressReporters()
    {
        var albumLookup = Albums.ToDictionary(a => a.Name);

        var log = new Progress<string>(line =>
            Dispatcher.UIThread.Post(() => LogLines.Add(line)));

        var albumProgress = new Progress<AlbumUploadProgress>(update =>
            Dispatcher.UIThread.Post(() =>
            {
                if (albumLookup.TryGetValue(update.AlbumName, out var album))
                {
                    album.UploadedCount += update.UploadedDelta;
                }
            }));

        return (log, albumProgress);
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
            var hasError = files.Any(f => state.SkippedFiles.Contains(f));

            Albums.Add(new AlbumProgress { Name = albumName, UploadedCount = uploaded, TotalCount = files.Count, HasError = hasError });
        }

        ApplyAlbumFilter();
    }
}
