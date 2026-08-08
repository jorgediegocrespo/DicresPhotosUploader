using GooglePhotosUploader.Config;
using GooglePhotosUploader.State;

namespace GooglePhotosUploader.Google;

public record UploadRunSummary(
    bool Success,
    int UploadedThisRun,
    int SkippedFilesTotal,
    int UploadedFilesTotal,
    int UsageCountToday,
    bool QuotaExceeded,
    string? ErrorMessage);

/// <summary>
/// Reusable upload logic (same as the original console app), designed to
/// be invoked both from the UI and from a headless run scheduled by the OS.
/// </summary>
public class UploadService
{
    public async Task<UploadRunSummary> RunAsync(
        AppConfig appConfig,
        StateStore stateStore,
        AppState state,
        IProgress<string> log,
        CancellationToken ct)
    {
        if (!Directory.Exists(appConfig.RootFolder))
        {
            var message = $"The root folder '{appConfig.RootFolder}' does not exist.";
            log.Report($"ERROR: {message}");
            return new UploadRunSummary(false, 0, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, message);
        }

        ResetDailyCounterIfNewDay(state);

        var erroredRoot = Path.GetFullPath(appConfig.ErroredFolderPath);
        var totalUploadedThisRun = 0;
        var runFailures = new List<(string AlbumName, string FileName, string Reason)>();

        try
        {
            var credential = await AuthHelper.GetCredentialAsync(appConfig.ClientSecretsPath, appConfig.TokenStorePath);
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(5); // large videos can take a while

            var client = new PhotosApiClient(http, credential);

            var albumFolders = Directory.GetDirectories(appConfig.RootFolder)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();

            log.Report($"Found {albumFolders.Count} folders (= {albumFolders.Count} potential albums).");

            foreach (var folder in albumFolders)
            {
                ct.ThrowIfCancellationRequested();

                var albumName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar));

                if (!state.Albums.TryGetValue(albumName, out var albumId))
                {
                    log.Report($"Creating album '{albumName}'...");
                    albumId = await client.CreateAlbumAsync(albumName);
                    CountRequest(state);
                    state.Albums[albumName] = albumId;
                    stateStore.Save(state);
                }

                var files = Directory.GetFiles(folder)
                    .Where(f => appConfig.AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Where(f => !state.UploadedFiles.ContainsKey(f))
                    .Where(f => !state.SkippedFiles.Contains(f))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (files.Count == 0)
                {
                    log.Report($"Album '{albumName}': nothing pending.");
                    continue;
                }

                log.Report($"Album '{albumName}': {files.Count} pending files.");

                foreach (var batch in Chunk(files, appConfig.BatchSize))
                {
                    ct.ThrowIfCancellationRequested();

                    var uploadedTokens = new List<(string FilePath, string UploadToken)>();

                    foreach (var filePath in batch)
                    {
                        try
                        {
                            var token = await client.UploadBytesAsync(filePath);
                            CountRequest(state);
                            uploadedTokens.Add((filePath, token));
                        }
                        catch (QuotaExceededException)
                        {
                            throw; // Google responded 429: handled in the outer catch, stop right away.
                        }
                        catch (Exception ex)
                        {
                            RegisterFailure(state, filePath, ex.Message, albumName, erroredRoot, log, runFailures);
                        }
                    }

                    if (uploadedTokens.Count > 0)
                    {
                        var results = await client.BatchCreateMediaItemsAsync(albumId, uploadedTokens);
                        CountRequest(state);

                        foreach (var result in results)
                        {
                            var filePath = uploadedTokens.First(u => Path.GetFileName(u.FilePath) == result.FileName).FilePath;

                            if (result.Success && result.MediaItemId is not null)
                            {
                                state.UploadedFiles[filePath] = result.MediaItemId;
                                totalUploadedThisRun++;
                            }
                            else
                            {
                                RegisterFailure(state, filePath, result.ErrorMessage ?? "unknown failure while confirming the media item", albumName, erroredRoot, log, runFailures);
                            }
                        }
                    }

                    stateStore.Save(state);
                    log.Report($"  ... {state.UploadedFiles.Count} files uploaded in total (historical). Requests today: {state.UsageCount}.");
                }
            }

            stateStore.Save(state);

            log.Report("=== Summary of this run ===");
            log.Report($"Photos/videos uploaded in this run: {totalUploadedThisRun}");
            log.Report($"Photos/videos discarded (copied to '{erroredRoot}'): {state.SkippedFiles.Count}");
            log.Report($"Historical total uploaded: {state.UploadedFiles.Count}");
            log.Report($"API requests made today: {state.UsageCount}");
            ReportFailuresSummary(runFailures, log);

            return new UploadRunSummary(true, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, null);
        }
        catch (QuotaExceededException ex)
        {
            stateStore.Save(state);
            log.Report($"WARNING: {ex.Message}");
            log.Report("Progress has been saved. Relaunch the application later (or tomorrow) to continue.");
            ReportFailuresSummary(runFailures, log);
            return new UploadRunSummary(true, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, true, ex.Message);
        }
        catch (OperationCanceledException)
        {
            stateStore.Save(state);
            log.Report("Run cancelled by the user. Progress has been saved.");
            ReportFailuresSummary(runFailures, log);
            return new UploadRunSummary(false, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, "Cancelled by the user");
        }
        catch (Exception ex)
        {
            stateStore.Save(state);
            log.Report($"Unexpected ERROR: {ex}");
            ReportFailuresSummary(runFailures, log);
            return new UploadRunSummary(false, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, ex.Message);
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
        CancellationToken ct)
    {
        var erroredRoot = Path.GetFullPath(appConfig.ErroredFolderPath);

        if (!Directory.Exists(erroredRoot))
        {
            var message = $"The errored folder '{erroredRoot}' does not exist.";
            log.Report($"ERROR: {message}");
            return new UploadRunSummary(false, 0, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, message);
        }

        ResetDailyCounterIfNewDay(state);

        var totalUploadedThisRun = 0;
        var succeeded = new List<(string AlbumName, string FileName)>();
        var runFailures = new List<(string AlbumName, string FileName, string Reason)>();

        try
        {
            var credential = await AuthHelper.GetCredentialAsync(appConfig.ClientSecretsPath, appConfig.TokenStorePath);
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(5); // large videos can take a while

            var client = new PhotosApiClient(http, credential);

            var albumFolders = Directory.GetDirectories(erroredRoot)
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();

            log.Report($"Reprocessing errored files: found {albumFolders.Count} album folder(s) under '{erroredRoot}'.");

            foreach (var folder in albumFolders)
            {
                ct.ThrowIfCancellationRequested();

                var albumName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar));

                if (!state.Albums.TryGetValue(albumName, out var albumId))
                {
                    log.Report($"Creating album '{albumName}'...");
                    albumId = await client.CreateAlbumAsync(albumName);
                    CountRequest(state);
                    state.Albums[albumName] = albumId;
                    stateStore.Save(state);
                }

                var files = Directory.GetFiles(folder)
                    .Where(f => appConfig.AllowedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (files.Count == 0)
                {
                    continue;
                }

                log.Report($"Album '{albumName}': retrying {files.Count} errored file(s).");

                foreach (var batch in Chunk(files, appConfig.BatchSize))
                {
                    ct.ThrowIfCancellationRequested();

                    var uploadedTokens = new List<(string FilePath, string UploadToken)>();

                    foreach (var filePath in batch)
                    {
                        try
                        {
                            var token = await client.UploadBytesAsync(filePath);
                            CountRequest(state);
                            uploadedTokens.Add((filePath, token));
                        }
                        catch (QuotaExceededException)
                        {
                            throw; // Google responded 429: handled in the outer catch, stop right away.
                        }
                        catch (Exception ex)
                        {
                            RegisterReprocessFailure(state, appConfig, albumName, filePath, ex.Message, log, runFailures);
                        }
                    }

                    if (uploadedTokens.Count > 0)
                    {
                        var results = await client.BatchCreateMediaItemsAsync(albumId, uploadedTokens);
                        CountRequest(state);

                        foreach (var result in results)
                        {
                            var filePath = uploadedTokens.First(u => Path.GetFileName(u.FilePath) == result.FileName).FilePath;

                            if (result.Success && result.MediaItemId is not null)
                            {
                                RegisterReprocessSuccess(state, appConfig, albumName, filePath, result.MediaItemId, log, succeeded);
                                totalUploadedThisRun++;
                            }
                            else
                            {
                                RegisterReprocessFailure(state, appConfig, albumName, filePath, result.ErrorMessage ?? "unknown failure while confirming the media item", log, runFailures);
                            }
                        }
                    }

                    stateStore.Save(state);
                }
            }

            stateStore.Save(state);

            log.Report("=== Summary of this reprocess run ===");
            log.Report($"Photos/videos re-uploaded successfully: {succeeded.Count}");
            log.Report($"Photos/videos still failing (kept in '{erroredRoot}'): {runFailures.Count}");
            ReportSucceededSummary(succeeded, log);
            ReportFailuresSummary(runFailures, log);

            return new UploadRunSummary(true, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, null);
        }
        catch (QuotaExceededException ex)
        {
            stateStore.Save(state);
            log.Report($"WARNING: {ex.Message}");
            log.Report("Progress has been saved. Relaunch the application later (or tomorrow) to continue.");
            ReportSucceededSummary(succeeded, log);
            ReportFailuresSummary(runFailures, log);
            return new UploadRunSummary(true, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, true, ex.Message);
        }
        catch (OperationCanceledException)
        {
            stateStore.Save(state);
            log.Report("Reprocess run cancelled by the user. Progress has been saved.");
            ReportSucceededSummary(succeeded, log);
            ReportFailuresSummary(runFailures, log);
            return new UploadRunSummary(false, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, "Cancelled by the user");
        }
        catch (Exception ex)
        {
            stateStore.Save(state);
            log.Report($"Unexpected ERROR: {ex}");
            ReportSucceededSummary(succeeded, log);
            ReportFailuresSummary(runFailures, log);
            return new UploadRunSummary(false, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, ex.Message);
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

    private static void CountRequest(AppState state) => state.UsageCount++;

    /// <summary>
    /// On any upload failure the file is discarded right away (no retries): it's added
    /// to <see cref="AppState.SkippedFiles"/>, copied to the errored folder for manual
    /// review, and recorded in <paramref name="runFailures"/> so the run summary can list it.
    /// </summary>
    private static void RegisterFailure(AppState state, string filePath, string reason, string albumName, string erroredRoot, IProgress<string> log, List<(string AlbumName, string FileName, string Reason)> runFailures)
    {
        state.SkippedFiles.Add(filePath);
        var fileName = Path.GetFileName(filePath);
        runFailures.Add((albumName, fileName, reason));

        log.Report($"  ✗ Discarded: {fileName} ({reason})");
        CopyToErroredFolder(filePath, albumName, erroredRoot, log);
    }

    private static void ReportFailuresSummary(List<(string AlbumName, string FileName, string Reason)> runFailures, IProgress<string> log)
    {
        if (runFailures.Count == 0)
        {
            return;
        }

        log.Report($"Failed photos/videos in this run ({runFailures.Count}):");
        foreach (var failure in runFailures)
        {
            log.Report($"  - [{failure.AlbumName}] {failure.FileName}: {failure.Reason}");
        }
    }

    /// <summary>
    /// A file from the errored folder was re-uploaded successfully: it's removed from
    /// the errored folder, and the original (untouched) file under
    /// <see cref="AppConfig.RootFolder"/> is marked as uploaded so future runs skip it.
    /// </summary>
    private static void RegisterReprocessSuccess(AppState state, AppConfig appConfig, string albumName, string erroredFilePath, string mediaItemId, IProgress<string> log, List<(string AlbumName, string FileName)> succeeded)
    {
        var fileName = Path.GetFileName(erroredFilePath);
        var originalPath = Path.Combine(appConfig.RootFolder, albumName, fileName);

        state.UploadedFiles[originalPath] = mediaItemId;
        state.SkippedFiles.Remove(originalPath);
        succeeded.Add((albumName, fileName));

        log.Report($"  ✓ Re-uploaded successfully: {fileName}");

        try
        {
            File.Delete(erroredFilePath);
        }
        catch (Exception ex)
        {
            log.Report($"    ⚠ Could not remove '{erroredFilePath}' from the errored folder after a successful re-upload: {ex.Message}");
        }
    }

    /// <summary>A file from the errored folder failed again: it's left in place (never re-copied).</summary>
    private static void RegisterReprocessFailure(AppState state, AppConfig appConfig, string albumName, string erroredFilePath, string reason, IProgress<string> log, List<(string AlbumName, string FileName, string Reason)> runFailures)
    {
        var fileName = Path.GetFileName(erroredFilePath);
        var originalPath = Path.Combine(appConfig.RootFolder, albumName, fileName);

        state.SkippedFiles.Add(originalPath);
        runFailures.Add((albumName, fileName, reason));

        log.Report($"  ✗ Still failing, kept in the errored folder: {fileName} ({reason})");
    }

    private static void ReportSucceededSummary(List<(string AlbumName, string FileName)> succeeded, IProgress<string> log)
    {
        if (succeeded.Count == 0)
        {
            return;
        }

        log.Report($"Successfully re-uploaded photos/videos ({succeeded.Count}):");
        foreach (var item in succeeded)
        {
            log.Report($"  - [{item.AlbumName}] {item.FileName}");
        }
    }

    /// <summary>
    /// Copies (never moves) the photo that failed to errored/&lt;AlbumName&gt;/&lt;file&gt;
    /// so you can review or retry it by hand. The original stays untouched.
    /// </summary>
    private static void CopyToErroredFolder(string filePath, string albumName, string erroredRoot, IProgress<string> log)
    {
        try
        {
            var destDir = Path.Combine(erroredRoot, albumName);
            Directory.CreateDirectory(destDir);

            var destPath = Path.Combine(destDir, Path.GetFileName(filePath));
            File.Copy(filePath, destPath, overwrite: true);

            log.Report($"    → Copy saved to '{destPath}' for manual review (the original was not touched).");
        }
        catch (Exception ex)
        {
            log.Report($"    ⚠ Could not copy the failed file to '{erroredRoot}': {ex.Message}");
        }
    }

    private static IEnumerable<List<string>> Chunk(List<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
        {
            yield return items.GetRange(i, Math.Min(size, items.Count - i));
        }
    }
}
