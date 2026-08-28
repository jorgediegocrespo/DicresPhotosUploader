using DicresPhotosUploader.Google;

namespace DicresPhotosUploader.Tests.Google;

public class MimeTypeHelperTests
{
    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.JPG", "image/jpeg")]   // case-insensitive
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.gif", "image/gif")]
    [InlineData("photo.bmp", "image/bmp")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("photo.heic", "image/heic")]
    [InlineData("photo.heif", "image/heif")]
    [InlineData("photo.tiff", "image/tiff")]
    [InlineData("photo.tif", "image/tiff")]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("video.mov", "video/quicktime")]
    [InlineData("video.avi", "video/x-msvideo")]
    [InlineData("video.mkv", "video/x-matroska")]
    [InlineData("video.m4v", "video/x-m4v")]
    [InlineData("video.3gp", "video/3gpp")]
    [InlineData("video.wmv", "video/x-ms-wmv")]
    public void GetMimeType_KnownExtension_ReturnsCorrectMime(string filePath, string expectedMime)
    {
        Assert.Equal(expectedMime, MimeTypeHelper.GetMimeType(filePath));
    }

    [Theory]
    [InlineData("file.txt")]
    [InlineData("file.pdf")]
    [InlineData("file.docx")]
    [InlineData("file")]
    public void GetMimeType_UnknownExtension_ReturnsFallback(string filePath)
    {
        Assert.Equal("application/octet-stream", MimeTypeHelper.GetMimeType(filePath));
    }

    [Fact]
    public void GetMimeType_FullPath_ReturnsCorrectMime()
    {
        var path = Path.Combine("some", "nested", "folder", "photo.png");
        Assert.Equal("image/png", MimeTypeHelper.GetMimeType(path));
    }
}
