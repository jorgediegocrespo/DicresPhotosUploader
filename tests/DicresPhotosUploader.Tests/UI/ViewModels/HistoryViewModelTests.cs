using DicresPhotosUploader.State;
using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class HistoryViewModelTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private RunHistoryEntry MakeEntry(RunOrigin origin, RunStatus status, DateTime startedUtc) =>
        new()
        {
            StartedUtc = startedUtc,
            FinishedUtc = startedUtc.AddSeconds(10),
            Origin = origin,
            Status = status,
            UploadedThisRun = 3,
            UploadedFilesTotal = 30,
            SkippedFilesTotal = 1,
            ErrorMessage = status == RunStatus.Error ? "boom" : null
        };

    [Fact]
    public void Constructor_EmptyHistory_EntriesIsEmpty()
    {
        var historyStore = new RunHistoryStore(_tempPath);
        var vm = new HistoryViewModel(historyStore);

        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void Constructor_LoadsEntriesOrderedByMostRecentFirst()
    {
        var historyStore = new RunHistoryStore(_tempPath);
        historyStore.Append(MakeEntry(RunOrigin.Manual, RunStatus.Ok, new DateTime(2025, 1, 1)));
        historyStore.Append(MakeEntry(RunOrigin.Scheduled, RunStatus.Error, new DateTime(2025, 1, 3)));
        historyStore.Append(MakeEntry(RunOrigin.Manual, RunStatus.QuotaExceeded, new DateTime(2025, 1, 2)));

        var vm = new HistoryViewModel(historyStore);

        Assert.Equal(3, vm.Entries.Count);
        // Most recent (Jan 3) should be first.
        Assert.Contains("boom", vm.Entries[0].ErrorMessage);
    }

    [Fact]
    public void RefreshCommand_ReloadsEntriesFromStore()
    {
        var historyStore = new RunHistoryStore(_tempPath);
        var vm = new HistoryViewModel(historyStore);
        Assert.Empty(vm.Entries);

        historyStore.Append(MakeEntry(RunOrigin.Manual, RunStatus.Ok, DateTime.UtcNow));
        vm.RefreshCommand.Execute(null);

        Assert.Single(vm.Entries);
    }

    [Fact]
    public void Refresh_MapsUploadedAndSkippedCounts()
    {
        var historyStore = new RunHistoryStore(_tempPath);
        historyStore.Append(MakeEntry(RunOrigin.Manual, RunStatus.Ok, DateTime.UtcNow));

        var vm = new HistoryViewModel(historyStore);

        var row = Assert.Single(vm.Entries);
        Assert.Equal(3, row.UploadedThisRun);
        Assert.Equal(30, row.UploadedFilesTotal);
        Assert.Equal(1, row.SkippedFilesTotal);
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
        var tmp = _tempPath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }
}
