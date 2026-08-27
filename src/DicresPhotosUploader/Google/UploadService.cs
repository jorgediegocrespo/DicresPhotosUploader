using DicresPhotosUploader.Config;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.State;

namespace DicresPhotosUploader.Google;

public record UploadRunSummary(
    bool Success,
    int UploadedThisRun,
    int SkippedFilesTotal,
    int UploadedFilesTotal,
    int UsageCountToday,
    bool QuotaExceeded,
    string? ErrorMessage);

/// <summary>Reported every time a file is confirmed uploaded, so the UI can update per-album progress live.</summary>
public record AlbumUploadProgress(string AlbumName, int UploadedDelta);

/// <summary>
/// Reusable upload logic, designed to
/// be invoked both from the UI and from a headless run scheduled by the OS.
/// </summary>
public class UploadService
{
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromMinutes(5); // large videos can take a while
    
    private sealed record PhotosSession(HttpClient Http, PhotosApiClient Client) : IDisposable
    {
        public void Dispose() => Http.Dispose();
    }

    public async Task<UploadRunSummary> RunAsync(
        AppConfig appConfig,
        StateStore stateStore,
        AppState state,
        IProgress<string> log,
        CancellationToken ct,
        IProgress<AlbumUploadProgress>? albumProgress = null)
    {
        var context = new RunContext(appConfig, stateStore, state, log, ct, albumProgress, "Upload_Cancelled");

        if (!Directory.Exists(appConfig.RootFolder))
        {
            return AbortMissingFolder(context, Loc.Format("Upload_RootFolderMissing", appConfig.RootFolder));
        }

        ResetDailyCounterIfNewDay(state);

        try
        {
            await UploadPendingAlbumsAsync(context);

            context.SaveState();
            ReportUploadSummary(context);
            return context.BuildSummary(success: true, quotaExceeded: false, errorMessage: null);
        }
        catch (Exception ex)
        {
            return HandleRunException(context, ex);
        }
    }

    /// <summary>
    /// Retries every file currently sitting in the errored folder, using it as the
    /// root (its subfolders are album names, matching the original layout). A file
    /// that succeeds is removed from the errored folder and marked as uploaded
    /// against its original path under <see cref="AppConfig.RootFolder"/>; a file
    /// that fails again is simply left in place.
    /// </summary>
    public async Task<UploadRunSummary> ReprocessErroredAsync(
        AppConfig appConfig,
        StateStore stateStore,
        AppState state,
        IProgress<string> log,
        CancellationToken ct,
        IProgress<AlbumUploadProgress>? albumProgress = null)
    {
        var context = new RunContext(appConfig, stateStore, state, log, ct, albumProgress, "Upload_ReprocessCancelled");

        if (!Directory.Exists(context.ErroredRoot))
        {
            return AbortMissingFolder(context, Loc.Format("Upload_ErroredFolderMissing", context.ErroredRoot));
        }

        ResetDailyCounterIfNewDay(state);

        try
        {
            await ReprocessErroredAlbumsAsync(context);

            context.SaveState();
            ReportReprocessSummary(context);
            return context.BuildSummary(success: true, quotaExceeded: false, errorMessage: null);
        }
        catch (Exception ex)
        {
            return HandleRunException(context, ex);
        }
    }

    private static async Task UploadPendingAlbumsAsync(RunContext context)
    {
        using var session = await OpenSessionAsync(context.Config);

        var albumFolders = GetAlbumFolders(context.Config.RootFolder);
        context.Log.Report(Loc.Format("Upload_FoundFolders", albumFolders.Count));

        foreach (var folder in albumFolders)
        {
            context.Ct.ThrowIfCancellationRequested();
            await UploadAlbumAsync(context, session.Client, folder);
        }
    }

    private static async Task UploadAlbumAsync(RunContext context, PhotosApiClient client, string folder)
    {
        var albumName = GetAlbumName(folder);
        var albumId = await EnsureAlbumAsync(context, client, albumName);
        var files = GetPendingFiles(context, folder);

        if (files.Count == 0)
        {
            context.Log.Report(Loc.Format("Upload_AlbumNothingPending", albumName));
            return;
        }

        context.Log.Report(Loc.Format("Upload_AlbumPendingFiles", albumName, files.Count));

        await ProcessAlbumBatchesAsync(
            context,
            client,
            albumName,
            albumId,
            files,
            onSuccess: (filePath, mediaItemId) => context.State.UploadedFiles[filePath] = mediaItemId,
            onFailure: (filePath, reason) => RegisterFailure(context, albumName, filePath, reason),
            onBatchCompleted: () => context.Log.Report(
                Loc.Format("Upload_ProgressUploaded", context.State.UploadedFiles.Count, context.State.UsageCount)));
    }

    private static async Task ReprocessErroredAlbumsAsync(RunContext context)
    {
        using var session = await OpenSessionAsync(context.Config);

        var albumFolders = GetAlbumFolders(context.ErroredRoot);
        context.Log.Report(Loc.Format("Upload_ReprocessFound", albumFolders.Count, context.ErroredRoot));

        foreach (var folder in albumFolders)
        {
            context.Ct.ThrowIfCancellationRequested();
            await ReprocessErroredAlbumAsync(context, session.Client, folder);
        }
    }

    private static async Task ReprocessErroredAlbumAsync(RunContext context, PhotosApiClient client, string folder)
    {
        var albumName = GetAlbumName(folder);
        var albumId = await EnsureAlbumAsync(context, client, albumName);
        var files = GetErroredFiles(context, folder);

        if (files.Count == 0)
        {
            return;
        }

        context.Log.Report(Loc.Format("Upload_ReprocessRetrying", albumName, files.Count));

        await ProcessAlbumBatchesAsync(
            context,
            client,
            albumName,
            albumId,
            files,
            onSuccess: (filePath, mediaItemId) => RegisterReprocessSuccess(context, albumName, filePath, mediaItemId),
            onFailure: (filePath, reason) => RegisterReprocessFailure(context, albumName, filePath, reason));
    }

    private static async Task<PhotosSession> OpenSessionAsync(AppConfig appConfig)
    {
        var credential = await AuthHelper.GetCredentialAsync(appConfig.TokenStorePath);
        var http = new HttpClient { Timeout = HttpTimeout };
        return new PhotosSession(http, new PhotosApiClient(http, credential));
    }

    private static async Task<string> EnsureAlbumAsync(RunContext context, PhotosApiClient client, string albumName)
    {
        if (context.State.Albums.TryGetValue(albumName, out var albumId))
        {
            return albumId;
        }

        context.Log.Report(Loc.Format("Upload_CreatingAlbum", albumName));
        albumId = await client.CreateAlbumAsync(albumName);
        context.CountRequest();
        context.State.Albums[albumName] = albumId;
        context.SaveState();

        return albumId;
    }

    /// <summary>
    /// Uploads the album files in batches: first the raw bytes (one request per file),
    /// then a single batch call that confirms them as media items inside the album.
    /// </summary>
    private static async Task ProcessAlbumBatchesAsync(
        RunContext context,
        PhotosApiClient client,
        string albumName,
        string albumId,
        List<string> files,
        Action<string, string> onSuccess,
        Action<string, string> onFailure,
        Action? onBatchCompleted = null)
    {
        foreach (var batch in Chunk(files, context.Config.BatchSize))
        {
            context.Ct.ThrowIfCancellationRequested();

            var uploadedTokens = await UploadBatchBytesAsync(context, client, batch, onFailure);

            if (uploadedTokens.Count > 0)
            {
                await ConfirmBatchAsync(context, client, albumName, albumId, uploadedTokens, onSuccess, onFailure);
            }

            context.SaveState();
            onBatchCompleted?.Invoke();
        }
    }

    private static async Task<List<(string FilePath, string UploadToken)>> UploadBatchBytesAsync(
        RunContext context,
        PhotosApiClient client,
        List<string> batch,
        Action<string, string> onFailure)
    {
        var uploadedTokens = new List<(string FilePath, string UploadToken)>();

        foreach (var filePath in batch)
        {
            try
            {
                var token = await client.UploadBytesAsync(filePath);
                context.CountRequest();
                uploadedTokens.Add((filePath, token));
            }
            catch (QuotaExceededException)
            {
                throw; // Google responded 429: handled by the caller, stop right away.
            }
            catch (Exception ex)
            {
                onFailure(filePath, ex.Message);
            }
        }

        return uploadedTokens;
    }

    private static async Task ConfirmBatchAsync(
        RunContext context,
        PhotosApiClient client,
        string albumName,
        string albumId,
        List<(string FilePath, string UploadToken)> uploadedTokens,
        Action<string, string> onSuccess,
        Action<string, string> onFailure)
    {
        var results = await client.BatchCreateMediaItemsAsync(albumId, uploadedTokens);
        context.CountRequest();

        foreach (var result in results)
        {
            var filePath = uploadedTokens.First(u => Path.GetFileName(u.FilePath) == result.FileName).FilePath;

            if (result.Success && result.MediaItemId is not null)
            {
                onSuccess(filePath, result.MediaItemId);
                context.RegisterUploaded(albumName);
            }
            else
            {
                onFailure(filePath, result.ErrorMessage ?? Loc.Get("Upload_UnknownConfirmFailure"));
            }
        }
    }

    private static List<string> GetAlbumFolders(string root) =>
        Directory.GetDirectories(root)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string GetAlbumName(string folder) =>
        Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar));

    private static List<string> GetPendingFiles(RunContext context, string folder) =>
        GetAllowedFiles(context, folder)
            .Where(f => !context.State.UploadedFiles.ContainsKey(f))
            .Where(f => !context.State.SkippedFiles.Contains(f))
            .ToList();

    private static List<string> GetErroredFiles(RunContext context, string folder) =>
        GetAllowedFiles(context, folder).ToList();

    private static IEnumerable<string> GetAllowedFiles(RunContext context, string folder) =>
        Directory.GetFiles(folder)
            .Where(f => context.Config.AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<List<string>> Chunk(List<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
        {
            yield return items.GetRange(i, Math.Min(size, items.Count - i));
        }
    }

    private static void ResetDailyCounterIfNewDay(AppState state)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        if (state.UsageDate != today)
        {
            state.UsageDate = today;
            state.UsageCount = 0;
        }
    }

    /// <summary>
    /// On any upload failure the file is discarded right away (no retries): it's added
    /// to <see cref="AppState.SkippedFiles"/>, copied to the errored folder for manual
    /// review, and recorded in the run failures so the run summary can list it.
    /// </summary>
    private static void RegisterFailure(RunContext context, string albumName, string filePath, string reason)
    {
        context.State.SkippedFiles.Add(filePath);
        var fileName = Path.GetFileName(filePath);
        context.Failures.Add((albumName, fileName, reason));

        context.Log.Report(Loc.Format("Upload_Discarded", fileName, reason));
        CopyToErroredFolder(context, albumName, filePath);
    }

    /// <summary>
    /// A file from the errored folder was re-uploaded successfully: it's removed from
    /// the errored folder, and the original (untouched) file under
    /// <see cref="AppConfig.RootFolder"/> is marked as uploaded so future runs skip it.
    /// </summary>
    private static void RegisterReprocessSuccess(RunContext context, string albumName, string erroredFilePath, string mediaItemId)
    {
        var fileName = Path.GetFileName(erroredFilePath);
        var originalPath = Path.Combine(context.Config.RootFolder, albumName, fileName);

        context.State.UploadedFiles[originalPath] = mediaItemId;
        context.State.SkippedFiles.Remove(originalPath);
        context.Succeeded.Add((albumName, fileName));

        context.Log.Report(Loc.Format("Upload_ReuploadedSuccess", fileName));

        try
        {
            File.Delete(erroredFilePath);
        }
        catch (Exception ex)
        {
            context.Log.Report(Loc.Format("Upload_CouldNotRemoveErrored", erroredFilePath, ex.Message));
        }
    }

    /// <summary>A file from the errored folder failed again: it's left in place (never re-copied).</summary>
    private static void RegisterReprocessFailure(RunContext context, string albumName, string erroredFilePath, string reason)
    {
        var fileName = Path.GetFileName(erroredFilePath);
        var originalPath = Path.Combine(context.Config.RootFolder, albumName, fileName);

        context.State.SkippedFiles.Add(originalPath);
        context.Failures.Add((albumName, fileName, reason));

        context.Log.Report(Loc.Format("Upload_StillFailing", fileName, reason));
    }

    /// <summary>
    /// Copies (never moves) the photo that failed to errored/&lt;AlbumName&gt;/&lt;file&gt;
    /// so you can review or retry it by hand. The original stays untouched.
    /// </summary>
    private static void CopyToErroredFolder(RunContext context, string albumName, string filePath)
    {
        try
        {
            var destDir = Path.Combine(context.ErroredRoot, albumName);
            Directory.CreateDirectory(destDir);

            var destPath = Path.Combine(destDir, Path.GetFileName(filePath));
            File.Copy(filePath, destPath, overwrite: true);

            context.Log.Report(Loc.Format("Upload_CopySaved", destPath));
        }
        catch (Exception ex)
        {
            context.Log.Report(Loc.Format("Upload_CouldNotCopy", context.ErroredRoot, ex.Message));
        }
    }

    // ------------------------------------------------------------------- reporting

    private static UploadRunSummary AbortMissingFolder(RunContext context, string message)
    {
        context.Log.Report(Loc.Format("Upload_ErrorPrefix", message));
        return context.BuildSummary(success: false, quotaExceeded: false, errorMessage: message);
    }

    /// <summary>Common handling for the three ways a run can end badly: quota, cancellation and unexpected errors.</summary>
    private static UploadRunSummary HandleRunException(RunContext context, Exception exception)
    {
        context.SaveState();

        UploadRunSummary summary;
        switch (exception)
        {
            case QuotaExceededException quota:
                context.Log.Report(Loc.Format("Upload_QuotaWarning", quota.Message));
                context.Log.Report(Loc.Get("Upload_QuotaResume"));
                summary = context.BuildSummary(success: true, quotaExceeded: true, errorMessage: quota.Message);
                break;

            case OperationCanceledException:
                context.Log.Report(Loc.Get(context.CancelledLogKey));
                summary = context.BuildSummary(success: false, quotaExceeded: false, errorMessage: Loc.Get("Upload_CancelledMessage"));
                break;

            default:
                context.Log.Report(Loc.Format("Upload_UnexpectedError", exception));
                summary = context.BuildSummary(success: false, quotaExceeded: false, errorMessage: exception.Message);
                break;
        }

        ReportSucceededSummary(context);
        ReportFailuresSummary(context);
        return summary;
    }

    private static void ReportUploadSummary(RunContext context)
    {
        context.Log.Report(Loc.Get("Upload_SummaryHeader"));
        context.Log.Report(Loc.Format("Upload_SummaryUploaded", context.UploadedThisRun));
        context.Log.Report(Loc.Format("Upload_SummaryDiscarded", context.ErroredRoot, context.State.SkippedFiles.Count));
        context.Log.Report(Loc.Format("Upload_SummaryHistorical", context.State.UploadedFiles.Count));
        context.Log.Report(Loc.Format("Upload_SummaryApiRequests", context.State.UsageCount));
        ReportFailuresSummary(context);
    }

    private static void ReportReprocessSummary(RunContext context)
    {
        context.Log.Report(Loc.Get("Upload_ReprocessSummaryHeader"));
        context.Log.Report(Loc.Format("Upload_ReprocessSummaryReuploaded", context.Succeeded.Count));
        context.Log.Report(Loc.Format("Upload_ReprocessSummaryStillFailing", context.ErroredRoot, context.Failures.Count));
        ReportSucceededSummary(context);
        ReportFailuresSummary(context);
    }

    private static void ReportSucceededSummary(RunContext context)
    {
        if (context.Succeeded.Count == 0)
        {
            return;
        }

        context.Log.Report(Loc.Format("Upload_ReprocessSucceededHeader", context.Succeeded.Count));
        foreach (var item in context.Succeeded)
        {
            context.Log.Report(Loc.Format("Upload_SucceededLine", item.AlbumName, item.FileName));
        }
    }

    private static void ReportFailuresSummary(RunContext context)
    {
        if (context.Failures.Count == 0)
        {
            return;
        }

        context.Log.Report(Loc.Format("Upload_FailuresHeader", context.Failures.Count));
        foreach (var failure in context.Failures)
        {
            context.Log.Report(Loc.Format("Upload_FailureLine", failure.AlbumName, failure.FileName, failure.Reason));
        }
    }

    /// <summary>Everything a single run needs to carry around: inputs, progress counters and outcome lists.</summary>
    private sealed class RunContext(
        AppConfig config,
        StateStore stateStore,
        AppState state,
        IProgress<string> log,
        CancellationToken ct,
        IProgress<AlbumUploadProgress>? albumProgress,
        string cancelledLogKey)
    {
        public AppConfig Config { get; } = config;
        public AppState State { get; } = state;
        public IProgress<string> Log { get; } = log;
        public CancellationToken Ct { get; } = ct;
        public string ErroredRoot { get; } = Path.GetFullPath(config.ErroredFolderPath);
        public string CancelledLogKey { get; } = cancelledLogKey;

        public int UploadedThisRun { get; private set; }
        public List<(string AlbumName, string FileName)> Succeeded { get; } = [];
        public List<(string AlbumName, string FileName, string Reason)> Failures { get; } = [];

        public void SaveState() => stateStore.Save(State);

        public void CountRequest() => State.UsageCount++;

        public void RegisterUploaded(string albumName)
        {
            UploadedThisRun++;
            albumProgress?.Report(new AlbumUploadProgress(albumName, 1));
        }

        public UploadRunSummary BuildSummary(bool success, bool quotaExceeded, string? errorMessage) =>
            new(success, UploadedThisRun, State.SkippedFiles.Count, State.UploadedFiles.Count, State.UsageCount, quotaExceeded, errorMessage);
    }
}
