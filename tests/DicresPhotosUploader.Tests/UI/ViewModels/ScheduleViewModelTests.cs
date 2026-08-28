using DicresPhotosUploader.Config;
using DicresPhotosUploader.Scheduling;
using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class ScheduleViewModelTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _configPath;

    public ScheduleViewModelTests()
    {
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    private (ConfigStore Store, AppConfig Config) CreateStoreAndConfig(AppConfig? config = null)
    {
        var store = new ConfigStore(_configPath);
        var appConfig = config ?? new AppConfig { TokenStorePath = Path.Combine(_tempDir, "token_store") };
        return (store, appConfig);
    }

    [Fact]
    public void Days_ContainsAllSevenDaysOfTheWeek()
    {
        var (store, config) = CreateStoreAndConfig();
        var vm = new ScheduleViewModel(store, config);

        Assert.Equal(7, vm.Days.Count);
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            Assert.Contains(vm.Days, d => d.Day == day);
        }
    }

    [Fact]
    public void Constructor_NoOAuthToken_IsOAuthReadyIsFalse()
    {
        var (store, config) = CreateStoreAndConfig();
        var vm = new ScheduleViewModel(store, config);

        Assert.False(vm.IsOAuthReady);
    }

    [Fact]
    public void Constructor_WithOAuthToken_IsOAuthReadyIsTrue()
    {
        var tokenStorePath = Path.Combine(_tempDir, "token_store");
        Directory.CreateDirectory(tokenStorePath);
        File.WriteAllText(Path.Combine(tokenStorePath, "token.json"), "{}");

        var (store, config) = CreateStoreAndConfig(new AppConfig { TokenStorePath = tokenStorePath });
        var vm = new ScheduleViewModel(store, config);

        Assert.True(vm.IsOAuthReady);
    }

    [Fact]
    public void Constructor_ExistingScheduleEntries_SelectsMatchingDaysAndTime()
    {
        var (store, config) = CreateStoreAndConfig(new AppConfig
        {
            TokenStorePath = Path.Combine(_tempDir, "token_store"),
            ScheduleEntries = new List<ScheduleEntry>
            {
                new() { DayOfWeek = DayOfWeek.Tuesday, Hour = 7, Minute = 45 }
            }
        });

        var vm = new ScheduleViewModel(store, config);

        Assert.True(vm.Days.Single(d => d.Day == DayOfWeek.Tuesday).IsSelected);
        Assert.All(vm.Days.Where(d => d.Day != DayOfWeek.Tuesday), d => Assert.False(d.IsSelected));
        Assert.Equal(new TimeSpan(7, 45, 0), vm.ScheduledTime);
    }

    [Fact]
    public void SaveAsync_WithoutOAuthReady_SetsNeedSignInStatus_AndDoesNotPersistSchedule()
    {
        var (store, config) = CreateStoreAndConfig();
        var vm = new ScheduleViewModel(store, config)
        {
            BackgroundScheduleEnabled = true
        };
        vm.Days[0].IsSelected = true;

        vm.SaveCommand.Execute(null);

        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
        Assert.Empty(store.Load().ScheduleEntries);
    }

    [Fact]
    public void SaveAsync_EnabledWithoutSelectedDays_SetsSelectDayStatus_AndDoesNotPersistSchedule()
    {
        var tokenStorePath = Path.Combine(_tempDir, "token_store");
        Directory.CreateDirectory(tokenStorePath);
        File.WriteAllText(Path.Combine(tokenStorePath, "token.json"), "{}");

        var (store, config) = CreateStoreAndConfig(new AppConfig { TokenStorePath = tokenStorePath });
        var vm = new ScheduleViewModel(store, config)
        {
            BackgroundScheduleEnabled = true
        };

        vm.SaveCommand.Execute(null);

        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
        Assert.Empty(store.Load().ScheduleEntries);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
