using CommunityToolkit.Mvvm.ComponentModel;
using DicresPhotosUploader.Localization;

namespace DicresPhotosUploader.UI.ViewModels;

// Must be observable (not a plain class) so the UI reflects UploadedCount changes
// as they happen during a run, instead of only when the Albums collection is rebuilt.
public partial class AlbumProgress : ObservableObject
{
    public required string Name { get; init; }

    [ObservableProperty]
    private int _uploadedCount;

    [ObservableProperty]
    private int _totalCount;

    /// <summary>Whether at least one file of this album was discarded after a failed upload.</summary>
    [ObservableProperty]
    private bool _hasError;

    public string ProgressText => Loc.Format("Dashboard_UploadedOfTotal", UploadedCount, TotalCount);

    /// <summary>Whether the album finished uploading all of its files with no failures.</summary>
    public bool IsCompletedSuccessfully => TotalCount > 0 && UploadedCount >= TotalCount && !HasError;

    partial void OnUploadedCountChanged(int value)
    {
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(IsCompletedSuccessfully));
    }

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(IsCompletedSuccessfully));
    }

    partial void OnHasErrorChanged(bool value) => OnPropertyChanged(nameof(IsCompletedSuccessfully));
}
