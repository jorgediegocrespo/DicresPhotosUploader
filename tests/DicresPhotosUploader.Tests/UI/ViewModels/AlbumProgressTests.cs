using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class AlbumProgressTests
{
    [Fact]
    public void IsCompletedSuccessfully_NoFilesYet_ReturnsFalse()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 0, UploadedCount = 0 };

        Assert.False(album.IsCompletedSuccessfully);
    }

    [Fact]
    public void IsCompletedSuccessfully_AllUploadedNoErrors_ReturnsTrue()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 3, UploadedCount = 3 };

        Assert.True(album.IsCompletedSuccessfully);
    }

    [Fact]
    public void IsCompletedSuccessfully_AllUploadedButHasError_ReturnsFalse()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 3, UploadedCount = 3, HasError = true };

        Assert.False(album.IsCompletedSuccessfully);
    }

    [Fact]
    public void IsCompletedSuccessfully_PartiallyUploaded_ReturnsFalse()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 3, UploadedCount = 1 };

        Assert.False(album.IsCompletedSuccessfully);
    }

    [Fact]
    public void ProgressText_ReflectsUploadedAndTotalCounts()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 10, UploadedCount = 4 };

        Assert.Contains("4", album.ProgressText);
        Assert.Contains("10", album.ProgressText);
    }

    [Fact]
    public void UploadedCountChanged_RaisesPropertyChangedForProgressTextAndIsCompletedSuccessfully()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 2, UploadedCount = 0 };
        var raisedProperties = new List<string>();
        album.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName!);

        album.UploadedCount = 2;

        Assert.Contains(nameof(AlbumProgress.ProgressText), raisedProperties);
        Assert.Contains(nameof(AlbumProgress.IsCompletedSuccessfully), raisedProperties);
    }

    [Fact]
    public void HasErrorChanged_RaisesPropertyChangedForIsCompletedSuccessfully()
    {
        var album = new AlbumProgress { Name = "Vacation", TotalCount = 2, UploadedCount = 2 };
        var raisedProperties = new List<string>();
        album.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName!);

        album.HasError = true;

        Assert.Contains(nameof(AlbumProgress.IsCompletedSuccessfully), raisedProperties);
    }
}
