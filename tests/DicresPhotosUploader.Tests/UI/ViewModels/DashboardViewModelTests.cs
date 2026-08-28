using DicresPhotosUploader.Config;
using DicresPhotosUploader.State;
using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class DashboardViewModelTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _statePath;
    private readonly string _historyPath;

    public DashboardViewModelTests()
    {
        Directory.CreateDirectory(_tempDir);
        _statePath = Path.Combine(_tempDir, "state.json");
        _historyPath = Path.Combine(_tempDir, "history.json");
    }

    private (AppConfig Config, StateStore StateStore, RunHistoryStore HistoryStore) CreateDependencies(string rootFolder)
    {
        var config = new AppConfig
        {
            RootFolder = rootFolder,
            AllowedExtensions = new[] { ".jpg" },
            ErroredFolderPath = Path.Combine(_tempDir, "errored")
        };
        return (config, new StateStore(_statePath), new RunHistoryStore(_historyPath));
    }

    [Fact]
    public void Constructor_MissingRootFolder_NoAlbums()
    {
        var (config, stateStore, historyStore) = CreateDependencies(Path.Combine(_tempDir, "does_not_exist"));

        var vm = new DashboardViewModel(config, stateStore, historyStore);

        Assert.Empty(vm.FilteredAlbums);
        Assert.Equal(0, vm.UploadedFilesTotal);
    }

    [Fact]
    public void Constructor_RootFolderWithAlbums_PopulatesFilteredAlbums()
    {
        var root = Path.Combine(_tempDir, "root");
        var album1 = Path.Combine(root, "Album1");
        var album2 = Path.Combine(root, "Album2");
        Directory.CreateDirectory(album1);
        Directory.CreateDirectory(album2);
        File.WriteAllText(Path.Combine(album1, "photo1.jpg"), "data");
        File.WriteAllText(Path.Combine(album2, "photo2.jpg"), "data");

        var (config, stateStore, historyStore) = CreateDependencies(root);

        var vm = new DashboardViewModel(config, stateStore, historyStore);

        Assert.Equal(2, vm.FilteredAlbums.Count);
        Assert.Contains(vm.FilteredAlbums, a => a.Name == "Album1" && a.TotalCount == 1);
        Assert.Contains(vm.FilteredAlbums, a => a.Name == "Album2" && a.TotalCount == 1);
    }

    [Fact]
    public void ShowOnlyAlbumsWithErrors_FiltersOutAlbumsWithoutErrors()
    {
        var root = Path.Combine(_tempDir, "root");
        var goodAlbum = Path.Combine(root, "Good");
        var badAlbum = Path.Combine(root, "Bad");
        Directory.CreateDirectory(goodAlbum);
        Directory.CreateDirectory(badAlbum);
        var goodFile = Path.Combine(goodAlbum, "ok.jpg");
        var badFile = Path.Combine(badAlbum, "bad.jpg");
        File.WriteAllText(goodFile, "data");
        File.WriteAllText(badFile, "data");

        var (config, stateStore, historyStore) = CreateDependencies(root);
        stateStore.Save(new AppState { SkippedFiles = new HashSet<string> { badFile } });

        var vm = new DashboardViewModel(config, stateStore, historyStore);

        vm.ShowOnlyAlbumsWithErrors = true;

        var remaining = Assert.Single(vm.FilteredAlbums);
        Assert.Equal("Bad", remaining.Name);
        Assert.True(remaining.HasError);
    }

    [Fact]
    public async Task RunNowAsync_MissingRootFolder_ReportsErrorAndAppendsHistoryEntry()
    {
        var (config, stateStore, historyStore) = CreateDependencies(Path.Combine(_tempDir, "does_not_exist"));
        var vm = new DashboardViewModel(config, stateStore, historyStore);

        await vm.RunNowCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.Single(historyStore.Load());
        Assert.Equal(RunStatus.Error, historyStore.Load()[0].Status);
    }

    [Fact]
    public async Task ReprocessErrorsAsync_MissingErroredFolder_ReportsErrorAndAppendsHistoryEntry()
    {
        var (config, stateStore, historyStore) = CreateDependencies(_tempDir);

        var vm = new DashboardViewModel(config, stateStore, historyStore);

        await vm.ReprocessErrorsCommand.ExecuteAsync(null);

        Assert.False(vm.IsRunning);
        Assert.Single(historyStore.Load());
        Assert.Equal(RunStatus.Error, historyStore.Load()[0].Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
