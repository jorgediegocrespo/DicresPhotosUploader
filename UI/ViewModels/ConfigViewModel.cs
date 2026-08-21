using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DicresPhotosUploader.Config;
using DicresPhotosUploader.Google;
using DicresPhotosUploader.Localization;

namespace DicresPhotosUploader.UI.ViewModels;

public record ThemeOption(string Key, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public record LanguageOption(string Key, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public partial class ConfigViewModel : ObservableObject
{
    private readonly ConfigStore _configStore;
    private readonly AppConfig _config;

    [ObservableProperty]
    private string _rootFolder;

    [ObservableProperty]
    private string _erroredFolderPath;

    [ObservableProperty]
    private int _batchSize;

    [ObservableProperty]
    private string _allowedExtensionsText;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isAuthorizing;

    /// <summary>True once the saved configuration is complete (root folder set and Google account authorized).</summary>
    [ObservableProperty]
    private bool _isConfigurationComplete;

    public List<ThemeOption> ThemeOptions { get; } = new()
    {
        new ThemeOption("System", Loc.Get("Theme_System")),
        new ThemeOption("Light", Loc.Get("Theme_Light")),
        new ThemeOption("Dark", Loc.Get("Theme_Dark"))
    };

    [ObservableProperty]
    private ThemeOption _selectedTheme;

    public List<LanguageOption> LanguageOptions { get; } = new()
    {
        new LanguageOption("System", Loc.Get("Language_System")),
        new LanguageOption("en-US", Loc.Get("Language_English")),
        new LanguageOption("es-ES", Loc.Get("Language_Spanish"))
    };

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public ConfigViewModel(ConfigStore configStore, AppConfig config)
    {
        _configStore = configStore;
        _config = config;

        _rootFolder = config.RootFolder;
        _erroredFolderPath = string.IsNullOrWhiteSpace(config.ErroredFolderPath)
            ? config.ErroredFolderPath
            : Path.GetFullPath(config.ErroredFolderPath);
        _batchSize = config.BatchSize;
        _allowedExtensionsText = string.Join(", ", config.AllowedExtensions);
        _selectedTheme = ThemeOptions.FirstOrDefault(t => t.Key == config.ThemePreference) ?? ThemeOptions[0];
        _selectedLanguage = LanguageOptions.FirstOrDefault(l => l.Key == config.LanguagePreference) ?? LanguageOptions[0];
        _isConfigurationComplete = ComputeIsConfigurationComplete();
    }

    /// <summary>A configuration is considered complete when a valid root folder is set and Google has been authorized.</summary>
    private bool ComputeIsConfigurationComplete() =>
        !string.IsNullOrWhiteSpace(_config.RootFolder)
        && Directory.Exists(_config.RootFolder)
        && !string.IsNullOrWhiteSpace(_config.ErroredFolderPath)
        && _config.AllowedExtensions.Length > 0
        && Directory.Exists(_config.TokenStorePath)
        && Directory.EnumerateFiles(_config.TokenStorePath).Any();

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        _config.ThemePreference = value.Key;
        App.ApplyTheme(value.Key);
        _configStore.Save(_config);
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        _config.LanguagePreference = value.Key;
        _configStore.Save(_config);
        StatusMessage = Loc.Get("Config_StatusLanguageChanged");
    }

    [RelayCommand]
    private void Save()
    {
        _config.RootFolder = RootFolder;
        _config.ErroredFolderPath = ErroredFolderPath;
        _config.BatchSize = BatchSize;
        _config.AllowedExtensions = AllowedExtensionsText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        _configStore.Save(_config);
        StatusMessage = Loc.Get("Config_StatusSaved");
        IsConfigurationComplete = ComputeIsConfigurationComplete();
    }

    [RelayCommand]
    private async Task ReauthorizeAsync()
    {
        try
        {
            IsAuthorizing = true;
            StatusMessage = Loc.Get("Config_StatusAuthorizing");

            if (Directory.Exists(_config.TokenStorePath))
            {
                Directory.Delete(_config.TokenStorePath, recursive: true);
            }

            await AuthHelper.GetCredentialAsync(_config.TokenStorePath);
            StatusMessage = Loc.Get("Config_StatusAuthorized");
        }
        catch (Exception ex)
        {
            StatusMessage = Loc.Format("Config_StatusAuthorizeError", ex.Message);
        }
        finally
        {
            IsAuthorizing = false;
            IsConfigurationComplete = ComputeIsConfigurationComplete();
        }
    }
}
