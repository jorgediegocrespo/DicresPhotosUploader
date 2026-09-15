namespace DicresPhotosUploader.Config;

public static class ConfigReadiness
{
    public static bool HasGoogleAuthorization(AppConfig config) =>
        Directory.Exists(config.TokenStorePath)
        && Directory.EnumerateFiles(config.TokenStorePath).Any();

    public static bool IsComplete(AppConfig config) =>
        !string.IsNullOrWhiteSpace(config.RootFolder)
        && Directory.Exists(config.RootFolder)
        && !string.IsNullOrWhiteSpace(config.ErroredFolderPath)
        && config.AllowedExtensions.Length > 0
        && HasGoogleAuthorization(config);
}
