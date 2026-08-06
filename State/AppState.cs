namespace GooglePhotosUploader.State;

public class AppState
{
    /// <summary>Folder name -> id of the album already created in Google Photos.</summary>
    public Dictionary<string, string> Albums { get; set; } = new();

    /// <summary>Full file path -> id of the media item already uploaded.</summary>
    public Dictionary<string, string> UploadedFiles { get; set; } = new();

    /// <summary>Number of consecutive failures per file (to stop retrying after several attempts).</summary>
    public Dictionary<string, int> FailureCounts { get; set; } = new();

    /// <summary>Files permanently discarded after too many failures.</summary>
    public HashSet<string> SkippedFiles { get; set; } = new();

    /// <summary>Date (yyyy-MM-dd, local time) of the current request counter.</summary>
    public string? UsageDate { get; set; }

    /// <summary>Requests made today against the API.</summary>
    public int UsageCount { get; set; }
}
