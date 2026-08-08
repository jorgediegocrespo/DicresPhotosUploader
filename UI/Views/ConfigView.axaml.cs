using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GooglePhotosUploader.UI.ViewModels;

namespace GooglePhotosUploader.UI.Views;

public partial class ConfigView : UserControl
{
    public ConfigView()
    {
        InitializeComponent();
    }

    private async void OnBrowseRootFolder(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ConfigViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the root folder"
        });

        if (folders.Count > 0)
        {
            vm.RootFolder = folders[0].Path.LocalPath;
        }
    }

    private async void OnBrowseClientSecret(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ConfigViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select client_secret.json",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
        });

        if (files.Count > 0)
        {
            vm.ClientSecretsPath = files[0].Path.LocalPath;
        }
    }

    private async void OnBrowseErroredFolder(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ConfigViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the discarded files folder"
        });

        if (folders.Count > 0)
        {
            vm.ErroredFolderPath = folders[0].Path.LocalPath;
        }
    }
}
