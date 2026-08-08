using Avalonia.Markup.Xaml;

namespace GooglePhotosUploader.Localization;

/// <summary>XAML markup extension that resolves a translated string at parse time: {loc:Tr SomeKey}.</summary>
public class TrExtension : MarkupExtension
{
    public string Key { get; set; }

    public TrExtension()
    {
        Key = string.Empty;
    }

    public TrExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.Get(Key);
}
