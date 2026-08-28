using DicresPhotosUploader.Config;
using DicresPhotosUploader.State;
using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class MainWindowViewModelTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public MainWindowViewModelTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    private (ConfigStore ConfigStore, AppConfig Config, StateStore StateStore, RunHistoryStore HistoryStore) CreateDependencies(bool configComplete)
    {
        var configStore = new ConfigStore(Path.Combine(_tempDir, "config.json"));
        var tokenStorePath = Path.Combine(_tempDir, "token_store");

        var config = new AppConfig
        {
            RootFolder = configComplete ? _tempDir : "",
            ErroredFolderPath = configComplete ? Path.Combine(_tempDir, "errored") : "",
            AllowedExtensions = configComplete ? new[] { ".jpg" } : Array.Empty<string>(),
            TokenStorePath = tokenStorePath
        };

        if (configComplete)
        {
            Directory.CreateDirectory(tokenStorePath);
            File.WriteAllText(Path.Combine(tokenStorePath, "token.json"), "{}");
        }

        var stateStore = new StateStore(Path.Combine(_tempDir, "state.json"));
        var historyStore = new RunHistoryStore(Path.Combine(_tempDir, "history.json"));

        return (configStore, config, stateStore, historyStore);
    }

    [Fact]
    public void Constructor_CreatesAllChildViewModels()
    {
        var (configStore, config, stateStore, historyStore) = CreateDependencies(configComplete: true);

        var vm = new MainWindowViewModel(configStore, config, stateStore, historyStore);

        Assert.NotNull(vm.Dashboard);
        Assert.NotNull(vm.Config);
        Assert.NotNull(vm.Schedule);
        Assert.NotNull(vm.History);
    }

    [Fact]
    public void Constructor_IncompleteConfiguration_SelectsConfigurationTab()
    {
        var (configStore, config, stateStore, historyStore) = CreateDependencies(configComplete: false);

        var vm = new MainWindowViewModel(configStore, config, stateStore, historyStore);

        Assert.Equal(1, vm.SelectedTabIndex);
    }

    [Fact]
    public void Constructor_CompleteConfiguration_SelectsDashboardTab()
    {
        var (configStore, config, stateStore, historyStore) = CreateDependencies(configComplete: true);

        var vm = new MainWindowViewModel(configStore, config, stateStore, historyStore);

        Assert.Equal(0, vm.SelectedTabIndex);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
