using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Aegis.Desktop.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(Login, CanLogin);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged();
                (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
                (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                (LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand LoginCommand { get; }

    private void Login()
    {
        // TODO: Implement actual login logic
        IsLoading = true;
        // Call API service
        IsLoading = false;
    }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !IsLoading;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
