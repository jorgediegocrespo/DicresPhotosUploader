using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DicresPhotosUploader.Localization;

namespace DicresPhotosUploader.UI.Views;

public partial class AboutWindow : Window
{
    private const string RepoUrl = "https://github.com/jorgediegocrespo/DicresPhotosUploader";
    private const string WebsiteUrl = "https://dicres.dev/side-projects/dicres-photos-uploader/";

    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = Loc.Format("About_Version", version?.ToString(3) ?? "1.0.0");
    }

    private void OnRepoLinkClick(object? sender, RoutedEventArgs e) => OpenUrl(RepoUrl);

    private void OnWebsiteLinkClick(object? sender, RoutedEventArgs e) => OpenUrl(WebsiteUrl);

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Best-effort: ignore failures to launch the default browser.
        }
    }
}
