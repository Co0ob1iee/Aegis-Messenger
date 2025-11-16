using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Aegis.Clients.Desktop.Views;

/// <summary>
/// Settings page with sub-navigation for different settings categories
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Default to Privacy settings
        SettingsContentFrame.Navigate(typeof(PrivacySettingsPage));
        SettingsNavView.SelectedItem = SettingsNavView.MenuItems[0];
    }

    private void SettingsNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer != null)
        {
            var tag = args.SelectedItemContainer.Tag?.ToString();

            switch (tag)
            {
                case "privacy":
                    SettingsContentFrame.Navigate(typeof(PrivacySettingsPage));
                    break;
                case "notifications":
                    // TODO: Navigate to NotificationsSettingsPage
                    break;
                case "account":
                    // TODO: Navigate to AccountSettingsPage
                    break;
                case "appearance":
                    // TODO: Navigate to AppearanceSettingsPage
                    break;
                case "storage":
                    // TODO: Navigate to StorageSettingsPage
                    break;
                case "about":
                    // TODO: Navigate to AboutPage
                    break;
            }
        }
    }
}
