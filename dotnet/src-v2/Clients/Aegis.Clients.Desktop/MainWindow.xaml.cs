using Aegis.Clients.Desktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aegis.Clients.Desktop;

/// <summary>
/// Main application window with NavigationView
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Title = "Aegis Messenger";

        // Navigate to default page
        ContentFrame.Navigate(typeof(ConversationsPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer != null)
        {
            var tag = args.SelectedItemContainer.Tag?.ToString();

            switch (tag)
            {
                case "conversations":
                    ContentFrame.Navigate(typeof(ConversationsPage));
                    break;
                case "contacts":
                    ContentFrame.Navigate(typeof(ContactsPage));
                    break;
                case "groups":
                    ContentFrame.Navigate(typeof(GroupsPage));
                    break;
            }
        }
    }

    private void NavView_SettingsInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // Navigate to Settings, which will show Privacy Settings as a sub-page
        ContentFrame.Navigate(typeof(SettingsPage));
    }
}
