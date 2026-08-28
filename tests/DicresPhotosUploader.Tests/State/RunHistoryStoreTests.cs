using DicresPhotosUploader.State;

namespace DicresPhotosUploader.Tests.State;

public class RunHistoryStoreTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private RunHistoryEntry MakeEntry(RunStatus status = RunStatus.Ok, int uploaded = 1) =>
        new()
        {
            StartedUtc = DateTime.UtcNow,
            FinishedUtc = DateTime.UtcNow.AddSeconds(10),
            Origin = RunOrigin.Manual,
            Status = status,
            UploadedThisRun = uploaded,
            UploadedFilesTotal = uploaded,
            SkippedFilesTotal = 0
        };

    [Fact]
    public void Load_MissingFile_ReturnsEmptyList()
    {
        File.Delete(_tempPath);
        var store = new RunHistoryStore(_tempPath);

        var entries = store.Load();

        Assert.Empty(entries);
    }

    [Fact]
    public void Append_SingleEntry_CanBeLoadedBack()
    {
        var store = new RunHistoryStore(_tempPath);
        var entry = MakeEntry(RunStatus.Ok, uploaded: 5);

        store.Append(entry);
        var loaded = store.Load();

        Assert.Single(loaded);
        Assert.Equal(RunStatus.Ok, loaded[0].Status);
        Assert.Equal(5, loaded[0].UploadedThisRun);
    }

    [Fact]
    public void Append_MultipleEntries_PreservesOrder()
    {
        var store = new RunHistoryStore(_tempPath);
        store.Append(MakeEntry(RunStatus.Ok, uploaded: 1));
        store.Append(MakeEntry(RunStatus.Error, uploaded: 2));
        store.Append(MakeEntry(RunStatus.QuotaExceeded, uploaded: 3));

        var loaded = store.Load();

        Assert.Equal(3, loaded.Count);
        Assert.Equal(RunStatus.Ok, loaded[0].Status);
        Assert.Equal(RunStatus.Error, loaded[1].Status);
        Assert.Equal(RunStatus.QuotaExceeded, loaded[2].Status);
    }

    [Fact]
    public void Append_ExceedsMaxEntries_OldestAreDropped()
    {
        var store = new RunHistoryStore(_tempPath);

        // Append 105 entries (max is 100).
        for (int i = 0; i < 105; i++)
        {
            store.Append(MakeEntry(uploaded: i));
        }

        var loaded = store.Load();

        Assert.Equal(100, loaded.Count);
        // The first kept entry should have uploaded = 5.
        Assert.Equal(5, loaded[0].UploadedThisRun);
        // The last entry should have uploaded = 104.
        Assert.Equal(104, loaded[^1].UploadedThisRun);
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
        var tmp = _tempPath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }
}
