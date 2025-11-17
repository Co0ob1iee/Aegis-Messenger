using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Aegis.Desktop.ViewModels;

public class PrivacySettingsViewModel : INotifyPropertyChanged
{
    private bool _sealedSenderEnabled;
    private bool _typingIndicatorsEnabled = true;
    private bool _readReceiptsEnabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PrivacySettingsViewModel()
    {
        SaveCommand = new RelayCommand(SaveSettings);
    }

    public bool SealedSenderEnabled
    {
        get => _sealedSenderEnabled;
        set
        {
            if (_sealedSenderEnabled != value)
            {
                _sealedSenderEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public bool TypingIndicatorsEnabled
    {
        get => _typingIndicatorsEnabled;
        set
        {
            if (_typingIndicatorsEnabled != value)
            {
                _typingIndicatorsEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ReadReceiptsEnabled
    {
        get => _readReceiptsEnabled;
        set
        {
            if (_readReceiptsEnabled != value)
            {
                _readReceiptsEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SaveCommand { get; }

    public event EventHandler<PrivacySettingsSavedEventArgs>? SettingsSaved;

    private void SaveSettings()
    {
        var settings = new PrivacySettingsSavedEventArgs
        {
            SealedSenderEnabled = SealedSenderEnabled,
            TypingIndicatorsEnabled = TypingIndicatorsEnabled,
            ReadReceiptsEnabled = ReadReceiptsEnabled
        };

        SettingsSaved?.Invoke(this, settings);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class PrivacySettingsSavedEventArgs : EventArgs
{
    public bool SealedSenderEnabled { get; set; }
    public bool TypingIndicatorsEnabled { get; set; }
    public bool ReadReceiptsEnabled { get; set; }
}
