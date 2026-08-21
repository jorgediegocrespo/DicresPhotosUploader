using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.UI.Views;

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
            Title = Loc.Get("Picker_SelectRootFolder")
        });

        if (folders.Count > 0)
        {
            vm.RootFolder = folders[0].Path.LocalPath;
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
            Title = Loc.Get("Picker_SelectErroredFolder")
        });

        if (folders.Count > 0)
        {
            vm.ErroredFolderPath = folders[0].Path.LocalPath;
        }
    }
}
