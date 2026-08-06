namespace GooglePhotosUploader.Google;

public static class MimeTypeHelper
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".heic"] = "image/heic",
        [".heif"] = "image/heif",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".mp4"] = "video/mp4",
        [".mov"] = "video/quicktime",
        [".avi"] = "video/x-msvideo",
        [".mkv"] = "video/x-matroska",
        [".m4v"] = "video/x-m4v",
        [".3gp"] = "video/3gpp",
        [".wmv"] = "video/x-ms-wmv",
    };

    public static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return Map.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }
}
