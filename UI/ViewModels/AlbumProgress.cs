using GooglePhotosUploader.Localization;

namespace GooglePhotosUploader.UI.ViewModels;

public class AlbumProgress
{
    public required string Name { get; init; }
    public int UploadedCount { get; init; }
    public int TotalCount { get; init; }

    public string ProgressText => Loc.Format("Dashboard_UploadedOfTotal", UploadedCount, TotalCount);
}
