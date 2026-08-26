using Avalonia.Markup.Xaml;

namespace DicresPhotosUploader.Localization;

public class TrExtension : MarkupExtension
{
    private string Key { get; set; }

    // TODO: Remove
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
