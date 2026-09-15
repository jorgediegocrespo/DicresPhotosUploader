using System.Globalization;
using System.Linq;
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

        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;
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

    public static string FormatNumber(int value) => value.ToString("N0", CurrentCulture);
    public static string FormatNumber(long value) => value.ToString("N0", CurrentCulture);
    public static string FormatNumber(decimal value) => value.ToString("N", CurrentCulture);

    public static string Format(string key, params object?[] args)
    {
        var formattedArgs = args.Select(NormalizeFormatArgument).ToArray();
        return string.Format(CurrentCulture, Get(key), formattedArgs);
    }

    private static object? NormalizeFormatArgument(object? value) => value switch
    {
        int i => i.ToString("N0", CurrentCulture),
        long l => l.ToString("N0", CurrentCulture),
        short s => s.ToString("N0", CurrentCulture),
        ushort us => us.ToString("N0", CurrentCulture),
        uint ui => ui.ToString("N0", CurrentCulture),
        ulong ul => ul.ToString("N0", CurrentCulture),
        byte b => b.ToString("N0", CurrentCulture),
        sbyte sb => sb.ToString("N0", CurrentCulture),
        decimal d => d.ToString("N", CurrentCulture),
        double dbl => dbl.ToString("N", CurrentCulture),
        float f => f.ToString("N", CurrentCulture),
        _ => value
    };
}
