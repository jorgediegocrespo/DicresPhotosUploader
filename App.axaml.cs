using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using GooglePhotosUploader.Config;
using GooglePhotosUploader.Localization;
using GooglePhotosUploader.State;
using GooglePhotosUploader.UI.ViewModels;
using GooglePhotosUploader.UI.Views;

namespace GooglePhotosUploader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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

    /// <summary>Switches the app's theme variant ("System", "Light" or "Dark").</summary>
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
