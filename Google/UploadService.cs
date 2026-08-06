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
                            RegisterFailure(state, filePath, ex.Message, albumName, erroredRoot, log);
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
                                state.FailureCounts.Remove(filePath);
                                totalUploadedThisRun++;
                            }
                            else
                            {
                                RegisterFailure(state, filePath, result.ErrorMessage ?? "unknown failure while confirming the media item", albumName, erroredRoot, log);
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
            log.Report($"Photos/videos permanently discarded (copied to '{erroredRoot}'): {state.SkippedFiles.Count}");
            log.Report($"Historical total uploaded: {state.UploadedFiles.Count}");
            log.Report($"API requests made today: {state.UsageCount}");

            return new UploadRunSummary(true, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, null);
        }
        catch (QuotaExceededException ex)
        {
            stateStore.Save(state);
            log.Report($"WARNING: {ex.Message}");
            log.Report("Progress has been saved. Relaunch the application later (or tomorrow) to continue.");
            return new UploadRunSummary(true, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, true, ex.Message);
        }
        catch (OperationCanceledException)
        {
            stateStore.Save(state);
            log.Report("Run cancelled by the user. Progress has been saved.");
            return new UploadRunSummary(false, totalUploadedThisRun, state.SkippedFiles.Count, state.UploadedFiles.Count, state.UsageCount, false, "Cancelled by the user");
        }
        catch (Exception ex)
        {
            stateStore.Save(state);
            log.Report($"Unexpected ERROR: {ex}");
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

    private static void RegisterFailure(AppState state, string filePath, string reason, string albumName, string erroredRoot, IProgress<string> log)
    {
        state.FailureCounts.TryGetValue(filePath, out var count);
        count++;
        state.FailureCounts[filePath] = count;

        if (count >= 3)
        {
            state.SkippedFiles.Add(filePath);
            log.Report($"  ✗ Discarded after {count} failures: {Path.GetFileName(filePath)} ({reason})");
            CopyToErroredFolder(filePath, albumName, erroredRoot, log);
        }
        else
        {
            log.Report($"  ✗ Failure ({count}/3) on {Path.GetFileName(filePath)}: {reason}");
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
