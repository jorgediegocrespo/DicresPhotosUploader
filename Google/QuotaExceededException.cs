namespace DicresPhotosUploader.Google;

/// <summary>
/// Thrown when Google responds with 429 (daily quota exhausted) or when we
/// decide to stop ourselves after reaching the configured daily budget.
/// </summary>
public class QuotaExceededException : Exception
{
    public QuotaExceededException(string message) : base(message)
    {
    }
}
