using DicresPhotosUploader.Scheduling;

namespace DicresPhotosUploader.Config;

public class AppConfig
{
    public static string AppDataFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DicresPhotosUploader");

    /// <summary>
    /// Root folder. Each direct subfolder becomes a Google Photos album.
    /// </summary>
    public string RootFolder { get; set; } = "";

    /// <summary>
    /// File where the progress is saved (created albums, already uploaded files, quota used).
    /// </summary>
    public string StateFilePath { get; set; } = Path.Combine(AppDataFolder, "state.json");

    /// <summary>
    /// File where the run history (manual and scheduled) is saved.
    /// </summary>
    public string RunHistoryFilePath { get; set; } = Path.Combine(AppDataFolder, "run_history.json");

    /// <summary>
    /// Folder where Google.Apis.Auth stores the OAuth token so you don't need to
    /// sign in again every day.
    /// </summary>
    public string TokenStorePath { get; set; } = Path.Combine(AppDataFolder, "token_store");

    /// <summary>
    /// Folder where a copy of the photos that fail permanently is saved
    /// (after 3 retries), organized into subfolders per album. The original photo
    /// is NEVER deleted or moved from its source folder; this is just a copy
    /// so you can manually review/retry the ones that had issues.
    /// </summary>
    public string ErroredFolderPath { get; set; } = Path.Combine(AppDataFolder, "errored");

    /// <summary>
    /// Number of photos grouped in each call to mediaItems:batchCreate (max. 50 per the API).
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// File extensions considered valid photos/videos.
    /// </summary>
    public string[] AllowedExtensions { get; set; } =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".heic", ".heif", ".tiff", ".tif",
        ".mp4", ".mov", ".avi", ".mkv", ".m4v", ".3gp", ".wmv"
    };

    /// <summary>Days/times when the background upload should run.</summary>
    public List<ScheduleEntry> ScheduleEntries { get; set; } = new();

    /// <summary>If true, the OS task/agent is (or should be) registered.</summary>
    public bool BackgroundScheduleEnabled { get; set; }

    /// <summary>UI theme preference: "System", "Light" or "Dark".</summary>
    public string ThemePreference { get; set; } = "System";

    /// <summary>UI language preference: "System", "en-US" or "es-ES".</summary>
    public string LanguagePreference { get; set; } = "System";
}
