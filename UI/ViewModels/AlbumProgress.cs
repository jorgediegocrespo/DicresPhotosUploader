namespace GooglePhotosUploader.UI.ViewModels;

public class AlbumProgress
{
    public required string Name { get; init; }
    public int UploadedCount { get; init; }
    public int TotalCount { get; init; }
}
