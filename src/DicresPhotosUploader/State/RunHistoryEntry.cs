namespace DicresPhotosUploader.State;

public enum RunOrigin
{
    Manual,
    Scheduled
}

public enum RunStatus
{
    Ok,
    QuotaExceeded,
    Error,
    Cancelled
}

public class RunHistoryEntry
{
    public DateTime StartedUtc { get; set; }
    public DateTime FinishedUtc { get; set; }
    public RunOrigin Origin { get; set; }
    public RunStatus Status { get; set; }
    public int UploadedThisRun { get; set; }
    public int UploadedFilesTotal { get; set; }
    public int SkippedFilesTotal { get; set; }
    public string? ErrorMessage { get; set; }
}
