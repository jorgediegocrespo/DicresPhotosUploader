using System.Globalization;
using DicresPhotosUploader.Localization;

namespace DicresPhotosUploader.Tests.Localization;

public class LocTests
{
    [Fact]
    public void FormatNumber_UsesSpanishThousandsSeparator()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            Loc.Initialize("es-ES");

            Assert.Equal("2.365", Loc.FormatNumber(2365));
            Assert.Contains("2.365", Loc.Format("Dashboard_HistoricalTotal", 2365));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            Loc.Initialize("System");
        }
    }
}
