using DicresPhotosUploader.Config;
using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class ConfigViewModelTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _configPath;

    public ConfigViewModelTests()
    {
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    private (ConfigStore Store, AppConfig Config) CreateStoreAndConfig(AppConfig? config = null)
    {
        var store = new ConfigStore(_configPath);
        var appConfig = config ?? new AppConfig
        {
            RootFolder = "",
            ErroredFolderPath = "",
            BatchSize = 50,
            AllowedExtensions = Array.Empty<string>(),
            TokenStorePath = Path.Combine(_tempDir, "token_store")
        };

        return (store, appConfig);
    }

    [Fact]
    public void Constructor_InitializesPropertiesFromConfig()
    {
        var (store, config) = CreateStoreAndConfig(new AppConfig
        {
            RootFolder = _tempDir,
            ErroredFolderPath = Path.Combine(_tempDir, "errored"),
            BatchSize = 25,
            AllowedExtensions = new[] { ".jpg", ".png" },
            ThemePreference = "Dark",
            LanguagePreference = "es-ES"
        });

        var vm = new ConfigViewModel(store, config);

        Assert.Equal(_tempDir, vm.RootFolder);
        Assert.Equal(25, vm.BatchSize);
        Assert.Equal(".jpg, .png", vm.AllowedExtensionsText);
        Assert.Equal("Dark", vm.SelectedTheme.Key);
        Assert.Equal("es-ES", vm.SelectedLanguage.Key);
    }

    [Fact]
    public void Constructor_UnknownThemeAndLanguage_FallsBackToFirstOption()
    {
        var (store, config) = CreateStoreAndConfig(new AppConfig
        {
            ThemePreference = "Unknown",
            LanguagePreference = "Unknown"
        });

        var vm = new ConfigViewModel(store, config);

        Assert.Equal(vm.ThemeOptions[0], vm.SelectedTheme);
        Assert.Equal(vm.LanguageOptions[0], vm.SelectedLanguage);
    }

    [Fact]
    public void Constructor_IncompleteConfig_IsConfigurationCompleteIsFalse()
    {
        var (store, config) = CreateStoreAndConfig();

        var vm = new ConfigViewModel(store, config);

        Assert.False(vm.IsConfigurationComplete);
    }

    [Fact]
    public void Constructor_CompleteConfig_IsConfigurationCompleteIsTrue()
    {
        var tokenStorePath = Path.Combine(_tempDir, "token_store");
        Directory.CreateDirectory(tokenStorePath);
        File.WriteAllText(Path.Combine(tokenStorePath, "token.json"), "{}");

        var (store, config) = CreateStoreAndConfig(new AppConfig
        {
            RootFolder = _tempDir,
            ErroredFolderPath = Path.Combine(_tempDir, "errored"),
            AllowedExtensions = new[] { ".jpg" },
            TokenStorePath = tokenStorePath
        });

        var vm = new ConfigViewModel(store, config);

        Assert.True(vm.IsConfigurationComplete);
    }

    [Fact]
    public void Save_PersistsValuesAndUpdatesStatusMessage()
    {
        var (store, config) = CreateStoreAndConfig();
        var vm = new ConfigViewModel(store, config)
        {
            RootFolder = _tempDir,
            ErroredFolderPath = Path.Combine(_tempDir, "errored"),
            BatchSize = 10,
            AllowedExtensionsText = ".jpg, .png"
        };

        vm.SaveCommand.Execute(null);

        var reloaded = store.Load();
        Assert.Equal(_tempDir, reloaded.RootFolder);
        Assert.Equal(10, reloaded.BatchSize);
        Assert.Equal(new[] { ".jpg", ".png" }, reloaded.AllowedExtensions);
        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
    }

    [Fact]
    public void Save_RecomputesIsConfigurationComplete()
    {
        var tokenStorePath = Path.Combine(_tempDir, "token_store");
        Directory.CreateDirectory(tokenStorePath);
        File.WriteAllText(Path.Combine(tokenStorePath, "token.json"), "{}");

        var (store, config) = CreateStoreAndConfig(new AppConfig { TokenStorePath = tokenStorePath });
        var vm = new ConfigViewModel(store, config);
        Assert.False(vm.IsConfigurationComplete);

        vm.RootFolder = _tempDir;
        vm.ErroredFolderPath = Path.Combine(_tempDir, "errored");
        vm.AllowedExtensionsText = ".jpg";
        vm.SaveCommand.Execute(null);

        Assert.True(vm.IsConfigurationComplete);
    }

    [Fact]
    public void SelectedLanguageChanged_PersistsPreferenceAndUpdatesStatusMessage()
    {
        var (store, config) = CreateStoreAndConfig();
        var vm = new ConfigViewModel(store, config);

        vm.SelectedLanguage = vm.LanguageOptions.First(l => l.Key == "es-ES");

        var reloaded = store.Load();
        Assert.Equal("es-ES", reloaded.LanguagePreference);
        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
