using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DicresPhotosUploader.UI.ViewModels;

public partial class DayOption : ObservableObject
{
    public DayOfWeek Day { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool _isSelected;

    public DayOption(DayOfWeek day, string label)
    {
        Day = day;
        Label = label;
    }
}
