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

    public MainWindowViewModel(ConfigStore configStore, AppConfig config, StateStore stateStore, RunHistoryStore historyStore)
    {
        Dashboard = new DashboardViewModel(config, stateStore, historyStore);
        Config = new ConfigViewModel(configStore, config);
        Schedule = new ScheduleViewModel(configStore, config);
        History = new HistoryViewModel(historyStore);
    }
}
