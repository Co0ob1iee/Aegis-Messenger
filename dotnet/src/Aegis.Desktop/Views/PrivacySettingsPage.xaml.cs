using Microsoft.UI.Xaml.Controls;
using Aegis.Desktop.ViewModels;

namespace Aegis.Desktop.Views;

public sealed partial class PrivacySettingsPage : Page
{
    public PrivacySettingsViewModel ViewModel { get; }

    public PrivacySettingsPage()
    {
        this.InitializeComponent();
        ViewModel = new PrivacySettingsViewModel();
    }
}
