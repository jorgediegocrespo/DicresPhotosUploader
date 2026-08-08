using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GooglePhotosUploader.Localization;
using GooglePhotosUploader.State;

namespace GooglePhotosUploader.UI.ViewModels;

public class RunHistoryRow
{
    public required string Started { get; init; }
    public required string Origin { get; init; }
    public required string Status { get; init; }
    public int UploadedThisRun { get; init; }
    public int SkippedFilesTotal { get; init; }
    public int UploadedFilesTotal { get; init; }
    public string? ErrorMessage { get; init; }
}

public partial class HistoryViewModel : ObservableObject
{
    private readonly RunHistoryStore _historyStore;

    public ObservableCollection<RunHistoryRow> Entries { get; } = new();

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
            Entries.Add(new RunHistoryRow
            {
                Started = entry.StartedUtc.ToString(CultureInfo.CurrentCulture),
                Origin = Loc.Get(entry.Origin == RunOrigin.Manual ? "RunOrigin_Manual" : "RunOrigin_Scheduled"),
                Status = Loc.Get(entry.Status switch
                {
                    RunStatus.Ok => "RunStatus_Ok",
                    RunStatus.QuotaExceeded => "RunStatus_QuotaExceeded",
                    RunStatus.Cancelled => "RunStatus_Cancelled",
                    _ => "RunStatus_Error"
                }),
                UploadedThisRun = entry.UploadedThisRun,
                SkippedFilesTotal = entry.SkippedFilesTotal,
                UploadedFilesTotal = entry.UploadedFilesTotal,
                ErrorMessage = entry.ErrorMessage
            });
        }
    }
}
