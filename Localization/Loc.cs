using System.Globalization;
using System.Resources;

namespace DicresPhotosUploader.Localization;

/// <summary>
/// Resolves UI/log strings from the Strings.resx resource files.
/// Strings.resx holds the neutral (English) texts and Strings.&lt;culture&gt;.resx the translations,
/// so adding a language only requires a new .resx file.
/// </summary>
public static class Loc
{
    private static readonly ResourceManager Resources =
        new("DicresPhotosUploader.Localization.Strings", typeof(Loc).Assembly);

    private static CultureInfo CurrentCulture { get; set; } = CultureInfo.InvariantCulture;

    public static void Initialize(string languagePreference = "System")
    {
        CurrentCulture = languagePreference switch
        {
            "System" or "" or null => CultureInfo.CurrentUICulture,
            _ => ResolveCulture(languagePreference)
        };
    }

    private static CultureInfo ResolveCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture;
        }
    }

    public static string Get(string key) => Resources.GetString(key, CurrentCulture) ?? key;

    public static string Format(string key, params object?[] args) => string.Format(Get(key), args);
}
