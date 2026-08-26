namespace DicresPhotosUploader.State;

public class AppState
{
    public Dictionary<string, string> Albums { get; set; } = new();
    public Dictionary<string, string> UploadedFiles { get; set; } = new();
    public HashSet<string> SkippedFiles { get; set; } = new();
    public string? UsageDate { get; set; }
    public int UsageCount { get; set; }
}
