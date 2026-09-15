using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace DicresPhotosUploader.UI.Controls;

/// <summary>
/// Button that replaces its content with an activity indicator while its action runs.
/// The busy state is either set explicitly through <see cref="IsBusy"/> or detected
/// automatically when the bound command is an <see cref="IAsyncRelayCommand"/> that is
/// still executing. While busy the button is disabled so the action cannot be launched twice.
/// </summary>
public class LoadingButton : Button
{
    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<LoadingButton, bool>(nameof(IsBusy));

    public static readonly DirectProperty<LoadingButton, bool> IsSpinnerVisibleProperty =
        AvaloniaProperty.RegisterDirect<LoadingButton, bool>(nameof(IsSpinnerVisible), o => o.IsSpinnerVisible);

    private bool _isSpinnerVisible;
    private IAsyncRelayCommand? _trackedCommand;

    public bool IsBusy
    {
        get => GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public bool IsSpinnerVisible
    {
        get => _isSpinnerVisible;
        private set
        {
            if (SetAndRaise(IsSpinnerVisibleProperty, ref _isSpinnerVisible, value))
            {
                PseudoClasses.Set(":busy", value);
                UpdateIsEffectivelyEnabled();
            }
        }
    }

    protected override Type StyleKeyOverride => typeof(LoadingButton);

    protected override bool IsEnabledCore => base.IsEnabledCore && !IsSpinnerVisible;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsBusyProperty)
        {
            UpdateSpinnerVisibility();
        }
        else if (change.Property == CommandProperty)
        {
            TrackCommand(change.GetNewValue<ICommand?>());
        }
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TrackCommand(null);
    }

    private void TrackCommand(ICommand? command)
    {
        if (_trackedCommand is not null)
        {
            _trackedCommand.PropertyChanged -= OnTrackedCommandPropertyChanged;
        }

        _trackedCommand = command as IAsyncRelayCommand;

        if (_trackedCommand is not null)
        {
            _trackedCommand.PropertyChanged += OnTrackedCommandPropertyChanged;
        }

        UpdateSpinnerVisibility();
    }

    private void OnTrackedCommandPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(IAsyncRelayCommand.IsRunning))
        {
            UpdateSpinnerVisibility();
        }
    }

    private void UpdateSpinnerVisibility() => IsSpinnerVisible = IsBusy || _trackedCommand?.IsRunning == true;
}
