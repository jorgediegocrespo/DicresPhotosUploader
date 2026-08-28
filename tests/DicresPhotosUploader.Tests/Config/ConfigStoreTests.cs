using DicresPhotosUploader.Config;
using DicresPhotosUploader.Scheduling;

namespace DicresPhotosUploader.Tests.Config;

public class ConfigStoreTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [Fact]
    public void Load_MissingFile_ReturnsDefaultConfig()
    {
        File.Delete(_tempPath); // ensure the file doesn't exist
        var store = new ConfigStore(_tempPath);

        var config = store.Load();

        Assert.NotNull(config);
        Assert.Equal("", config.RootFolder);
        Assert.Equal("System", config.ThemePreference);
        Assert.Equal("System", config.LanguagePreference);
        Assert.Empty(config.ScheduleEntries);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllProperties()
    {
        var store = new ConfigStore(_tempPath);
        var original = new AppConfig
        {
            RootFolder = "/photos/root",
            BatchSize = 25,
            BackgroundScheduleEnabled = true,
            ThemePreference = "Dark",
            LanguagePreference = "es-ES",
            ScheduleEntries = new List<ScheduleEntry>
            {
                new() { DayOfWeek = DayOfWeek.Monday, Hour = 8, Minute = 30 }
            }
        };

        store.Save(original);
        var loaded = store.Load();

        Assert.Equal(original.RootFolder, loaded.RootFolder);
        Assert.Equal(original.BatchSize, loaded.BatchSize);
        Assert.Equal(original.BackgroundScheduleEnabled, loaded.BackgroundScheduleEnabled);
        Assert.Equal(original.ThemePreference, loaded.ThemePreference);
        Assert.Equal(original.LanguagePreference, loaded.LanguagePreference);
        Assert.Single(loaded.ScheduleEntries);
        Assert.Equal(DayOfWeek.Monday, loaded.ScheduleEntries[0].DayOfWeek);
        Assert.Equal(8, loaded.ScheduleEntries[0].Hour);
        Assert.Equal(30, loaded.ScheduleEntries[0].Minute);
    }

    [Fact]
    public void Save_WritesAtomically_ExistingFileIsReplaced()
    {
        var store = new ConfigStore(_tempPath);
        var first = new AppConfig { RootFolder = "/first" };
        var second = new AppConfig { RootFolder = "/second" };

        store.Save(first);
        store.Save(second);
        var loaded = store.Load();

        Assert.Equal("/second", loaded.RootFolder);
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
        var tmp = _tempPath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }
}
