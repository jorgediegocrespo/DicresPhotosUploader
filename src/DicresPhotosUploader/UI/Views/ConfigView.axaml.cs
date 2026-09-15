using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.UI.Controls;
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
        await BrowseFolderAsync(sender, "Picker_SelectRootFolder", (vm, path) => vm.RootFolder = path);
    }

    private async void OnBrowseErroredFolder(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await BrowseFolderAsync(sender, "Picker_SelectErroredFolder", (vm, path) => vm.ErroredFolderPath = path);
    }

    private async Task BrowseFolderAsync(object? sender, string titleKey, Action<ConfigViewModel, string> applyFolder)
    {
        var button = sender as LoadingButton;

        if (button is not null)
        {
            button.IsBusy = true;
        }

        try
        {
            await PickFolderAsync(titleKey, applyFolder);
        }
        finally
        {
            if (button is not null)
            {
                button.IsBusy = false;
            }
        }
    }

    private async Task PickFolderAsync(string titleKey, Action<ConfigViewModel, string> applyFolder)
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
            Title = Loc.Get(titleKey)
        });

        if (folders.Count > 0)
        {
            applyFolder(vm, folders[0].Path.LocalPath);
        }
    }
}
