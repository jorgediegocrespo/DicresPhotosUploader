using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DicresPhotosUploader.Config;
using DicresPhotosUploader.Localization;
using DicresPhotosUploader.State;
using DicresPhotosUploader.UI.ViewModels;
using DicresPhotosUploader.UI.Views;

namespace DicresPhotosUploader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AppAbout_OnClick(object? sender, System.EventArgs e)
    {
        var about = new AboutWindow();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
        {
            about.ShowDialog(mainWindow);
        }
        else
        {
            about.Show();
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Directory.CreateDirectory(AppConfig.AppDataFolder);

            var configStore = new ConfigStore();
            var config = configStore.Load();

            Loc.Initialize(config.LanguagePreference);
            ApplyTheme(config.ThemePreference);

            var stateStore = new StateStore(config.StateFilePath);
            var historyStore = new RunHistoryStore(config.RunHistoryFilePath);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(configStore, config, stateStore, historyStore)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    public static void ApplyTheme(string preference)
    {
        if (Current is null)
        {
            return;
        }

        Current.RequestedThemeVariant = preference switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
