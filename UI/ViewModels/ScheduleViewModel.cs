using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GooglePhotosUploader.Config;
using GooglePhotosUploader.Scheduling;

namespace GooglePhotosUploader.UI.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private readonly ConfigStore _configStore;
    private readonly AppConfig _config;
    private readonly IBackgroundScheduler? _scheduler;

    public List<DayOption> Days { get; } = new()
    {
        new DayOption(DayOfWeek.Monday, "Monday"),
        new DayOption(DayOfWeek.Tuesday, "Tuesday"),
        new DayOption(DayOfWeek.Wednesday, "Wednesday"),
        new DayOption(DayOfWeek.Thursday, "Thursday"),
        new DayOption(DayOfWeek.Friday, "Friday"),
        new DayOption(DayOfWeek.Saturday, "Saturday"),
        new DayOption(DayOfWeek.Sunday, "Sunday")
    };

    [ObservableProperty]
    private TimeSpan _scheduledTime = new(9, 0, 0);

    [ObservableProperty]
    private bool _backgroundScheduleEnabled;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isOAuthReady;

    public ScheduleViewModel(ConfigStore configStore, AppConfig config)
    {
        _configStore = configStore;
        _config = config;
        _scheduler = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? IBackgroundScheduler.Create() : null;

        BackgroundScheduleEnabled = config.BackgroundScheduleEnabled;
        IsOAuthReady = Directory.Exists(config.TokenStorePath) && Directory.EnumerateFileSystemEntries(config.TokenStorePath).Any();

        foreach (var entry in config.ScheduleEntries)
        {
            var day = Days.FirstOrDefault(d => d.Day == entry.DayOfWeek);
            if (day is not null)
            {
                day.IsSelected = true;
            }

            ScheduledTime = new TimeSpan(entry.Hour, entry.Minute, 0);
        }

        _ = RefreshStatusAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_scheduler is null)
        {
            StatusMessage = "Scheduled execution is only available on Windows and macOS.";
            return;
        }

        if (!IsOAuthReady)
        {
            StatusMessage = "First sign in with Google from the Configuration tab.";
            return;
        }

        var selectedDays = Days.Where(d => d.IsSelected).ToList();

        if (BackgroundScheduleEnabled && selectedDays.Count == 0)
        {
            StatusMessage = "Select at least one day.";
            return;
        }

        _config.ScheduleEntries = selectedDays
            .Select(d => new ScheduleEntry { DayOfWeek = d.Day, Hour = ScheduledTime.Hours, Minute = ScheduledTime.Minutes })
            .ToList();
        _config.BackgroundScheduleEnabled = BackgroundScheduleEnabled;
        _configStore.Save(_config);

        try
        {
            if (BackgroundScheduleEnabled)
            {
                var executablePath = Process.GetCurrentProcess().MainModule!.FileName;
                await _scheduler.RegisterAsync(_config.ScheduleEntries, executablePath);
                StatusMessage = $"Schedule saved. Approximate next run: {ScheduleCalculator.GetNextOccurrence(_config.ScheduleEntries):dd/MM/yyyy HH:mm}.";
            }
            else
            {
                await _scheduler.UnregisterAsync();
                StatusMessage = "Background execution disabled.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error registering the schedule: {ex.Message}";
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_scheduler is null)
        {
            return;
        }

        var registered = await _scheduler.IsRegisteredAsync();
        if (registered && _config.ScheduleEntries.Count > 0)
        {
            StatusMessage = $"Active. Approximate next run: {ScheduleCalculator.GetNextOccurrence(_config.ScheduleEntries):dd/MM/yyyy HH:mm}.";
        }
    }
}
