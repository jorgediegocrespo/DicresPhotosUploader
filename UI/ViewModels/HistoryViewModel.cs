using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GooglePhotosUploader.State;

namespace GooglePhotosUploader.UI.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly RunHistoryStore _historyStore;

    public ObservableCollection<RunHistoryEntry> Entries { get; } = new();

    public HistoryViewModel(RunHistoryStore historyStore)
    {
        _historyStore = historyStore;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Entries.Clear();
        foreach (var entry in _historyStore.Load().OrderByDescending(e => e.StartedUtc))
        {
            Entries.Add(entry);
        }
    }
}
