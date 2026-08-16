namespace DicresPhotosUploader.State;

public class AppState
{
    /// <summary>Folder name -> id of the album already created in Google Photos.</summary>
    public Dictionary<string, string> Albums { get; set; } = new();

    /// <summary>Full file path -> id of the media item already uploaded.</summary>
    public Dictionary<string, string> UploadedFiles { get; set; } = new();

    /// <summary>Files discarded after a failed upload (moved to the errored folder, never retried again).</summary>
    public HashSet<string> SkippedFiles { get; set; } = new();

    /// <summary>Date (yyyy-MM-dd, local time) of the current request counter.</summary>
    public string? UsageDate { get; set; }

    /// <summary>Requests made today against the API.</summary>
    public int UsageCount { get; set; }
}
