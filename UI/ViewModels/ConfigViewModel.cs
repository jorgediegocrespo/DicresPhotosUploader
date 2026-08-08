using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GooglePhotosUploader.Config;
using GooglePhotosUploader.Google;

namespace GooglePhotosUploader.UI.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    private readonly ConfigStore _configStore;
    private readonly AppConfig _config;

    [ObservableProperty]
    private string _rootFolder;

    [ObservableProperty]
    private string _clientSecretsPath;

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

    public ConfigViewModel(ConfigStore configStore, AppConfig config)
    {
        _configStore = configStore;
        _config = config;

        _rootFolder = config.RootFolder;
        _clientSecretsPath = config.ClientSecretsPath;
        _erroredFolderPath = string.IsNullOrWhiteSpace(config.ErroredFolderPath)
            ? config.ErroredFolderPath
            : Path.GetFullPath(config.ErroredFolderPath);
        _batchSize = config.BatchSize;
        _allowedExtensionsText = string.Join(", ", config.AllowedExtensions);
    }

    [RelayCommand]
    private void Save()
    {
        _config.RootFolder = RootFolder;
        _config.ClientSecretsPath = ClientSecretsPath;
        _config.ErroredFolderPath = ErroredFolderPath;
        _config.BatchSize = BatchSize;
        _config.AllowedExtensions = AllowedExtensionsText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        _configStore.Save(_config);
        StatusMessage = "Configuration saved.";
    }

    [RelayCommand]
    private async Task ReauthorizeAsync()
    {
        try
        {
            IsAuthorizing = true;
            StatusMessage = "Opening the browser to sign in with Google...";

            if (Directory.Exists(_config.TokenStorePath))
            {
                Directory.Delete(_config.TokenStorePath, recursive: true);
            }

            await AuthHelper.GetCredentialAsync(_config.ClientSecretsPath, _config.TokenStorePath);
            StatusMessage = "Google session started successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error signing in: {ex.Message}";
        }
        finally
        {
            IsAuthorizing = false;
        }
    }
}
