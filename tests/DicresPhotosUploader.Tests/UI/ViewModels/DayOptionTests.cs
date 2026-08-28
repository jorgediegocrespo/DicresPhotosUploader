using DicresPhotosUploader.UI.ViewModels;

namespace DicresPhotosUploader.Tests.UI.ViewModels;

public class DayOptionTests
{
    [Fact]
    public void Constructor_SetsDayAndLabel()
    {
        var option = new DayOption(DayOfWeek.Wednesday, "Wednesday");

        Assert.Equal(DayOfWeek.Wednesday, option.Day);
        Assert.Equal("Wednesday", option.Label);
    }

    [Fact]
    public void IsSelected_DefaultsToFalse()
    {
        var option = new DayOption(DayOfWeek.Monday, "Monday");

        Assert.False(option.IsSelected);
    }

    [Fact]
    public void IsSelected_CanBeToggled_AndRaisesPropertyChanged()
    {
        var option = new DayOption(DayOfWeek.Monday, "Monday");
        var raised = false;
        option.PropertyChanged += (_, e) => raised = e.PropertyName == nameof(DayOption.IsSelected);

        option.IsSelected = true;

        Assert.True(option.IsSelected);
        Assert.True(raised);
    }
}
