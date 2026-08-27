using DicresPhotosUploader.State;

namespace DicresPhotosUploader.Tests;

public class StateStoreTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [Fact]
    public void Load_MissingFile_ReturnsEmptyState()
    {
        File.Delete(_tempPath);
        var store = new StateStore(_tempPath);

        var state = store.Load();

        Assert.NotNull(state);
        Assert.Empty(state.Albums);
        Assert.Empty(state.UploadedFiles);
        Assert.Empty(state.SkippedFiles);
        Assert.Null(state.UsageDate);
        Assert.Equal(0, state.UsageCount);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllFields()
    {
        var store = new StateStore(_tempPath);
        var original = new AppState
        {
            Albums = new Dictionary<string, string> { ["Vacation"] = "album_id_1" },
            UploadedFiles = new Dictionary<string, string> { ["/photos/img.jpg"] = "media_id_1" },
            SkippedFiles = new HashSet<string> { "/photos/bad.jpg" },
            UsageDate = "2025-01-06",
            UsageCount = 42
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal("album_id_1", loaded.Albums["Vacation"]);
        Assert.Equal("media_id_1", loaded.UploadedFiles["/photos/img.jpg"]);
        Assert.Contains("/photos/bad.jpg", loaded.SkippedFiles);
        Assert.Equal("2025-01-06", loaded.UsageDate);
        Assert.Equal(42, loaded.UsageCount);
    }

    [Fact]
    public void Save_WritesAtomically_ExistingDataIsReplaced()
    {
        var store = new StateStore(_tempPath);
        var first = new AppState { UsageCount = 10 };
        var second = new AppState { UsageCount = 99 };

        store.Save(first);
        store.Save(second);
        var loaded = store.Load();

        Assert.Equal(99, loaded.UsageCount);
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
        var tmp = _tempPath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }
}
