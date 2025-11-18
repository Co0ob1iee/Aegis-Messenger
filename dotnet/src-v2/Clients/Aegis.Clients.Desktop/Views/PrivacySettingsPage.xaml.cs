using Aegis.Clients.Desktop.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Aegis.Clients.Desktop.Views;

/// <summary>
/// Privacy Settings page - allows users to configure privacy and anonymity options
/// </summary>
public sealed partial class PrivacySettingsPage : Page
{
    public PrivacySettingsViewModel ViewModel { get; }

    public PrivacySettingsPage()
    {
        this.InitializeComponent();

        // Get ViewModel from dependency injection (via App.xaml.cs or manual instantiation)
        ViewModel = App.GetService<PrivacySettingsViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Reload settings when navigating to page
        // ViewModel will auto-load in constructor
    }
}
