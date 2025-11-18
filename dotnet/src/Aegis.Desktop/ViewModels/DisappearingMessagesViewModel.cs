using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aegis.Desktop.Models;
using CommunityToolkit.Mvvm.Input;

namespace Aegis.Desktop.ViewModels;

public class DisappearingMessagesViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;
    private DisappearingMessageTimer? _selectedTimer;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DisappearingMessagesViewModel()
    {
        Timers = new ObservableCollection<DisappearingMessageTimer>(
            DisappearingMessageTimer.GetDefaultTimers());

        // Default to "Off"
        SelectedTimer = Timers[0];

        ApplyCommand = new RelayCommand(ApplySettings, CanApply);
    }

    public ObservableCollection<DisappearingMessageTimer> Timers { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDisabled));
                (ApplyCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsDisabled => !IsEnabled;

    public DisappearingMessageTimer? SelectedTimer
    {
        get => _selectedTimer;
        set
        {
            if (_selectedTimer != value)
            {
                _selectedTimer = value;
                OnPropertyChanged();
                (ApplyCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand ApplyCommand { get; }

    public event EventHandler<DisappearingMessagesSettingsEventArgs>? SettingsApplied;

    private void ApplySettings()
    {
        var settings = new DisappearingMessagesSettingsEventArgs
        {
            Enabled = IsEnabled,
            DisappearAfterSeconds = IsEnabled ? SelectedTimer?.Seconds : null
        };

        SettingsApplied?.Invoke(this, settings);
    }

    private bool CanApply()
    {
        return !IsEnabled || SelectedTimer != null;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DisappearingMessagesSettingsEventArgs : EventArgs
{
    public bool Enabled { get; set; }
    public int? DisappearAfterSeconds { get; set; }
}
