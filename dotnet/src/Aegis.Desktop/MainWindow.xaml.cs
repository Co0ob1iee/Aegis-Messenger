using Microsoft.UI.Xaml;
using Aegis.Desktop.ViewModels;

namespace Aegis.Desktop;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        // Get ViewModel from DI
        var app = Application.Current as App;
        ViewModel = app?.Host.Services.GetService(typeof(MainViewModel)) as MainViewModel
            ?? new MainViewModel();

        Title = "Aegis Messenger - Secure End-to-End Encrypted Messaging";
    }
}
