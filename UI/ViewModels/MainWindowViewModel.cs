using CommunityToolkit.Mvvm.ComponentModel;
using DicresPhotosUploader.Config;
using DicresPhotosUploader.State;

namespace DicresPhotosUploader.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public DashboardViewModel Dashboard { get; }
    public ConfigViewModel Config { get; }
    public ScheduleViewModel Schedule { get; }
    public HistoryViewModel History { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainWindowViewModel(ConfigStore configStore, AppConfig config, StateStore stateStore, RunHistoryStore historyStore)
    {
        Dashboard = new DashboardViewModel(config, stateStore, historyStore);
        Config = new ConfigViewModel(configStore, config);
        Schedule = new ScheduleViewModel(configStore, config);
        History = new HistoryViewModel(historyStore);

        // Tab order in MainWindow.axaml: 0 = Dashboard, 1 = Configuration, 2 = Schedule, 3 = History.
        SelectedTabIndex = Config.IsConfigurationComplete ? 0 : 1;
    }
}
