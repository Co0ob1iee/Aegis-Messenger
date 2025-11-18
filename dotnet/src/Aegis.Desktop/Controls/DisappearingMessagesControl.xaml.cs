using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Aegis.Desktop.ViewModels;

namespace Aegis.Desktop.Controls;

public sealed partial class DisappearingMessagesControl : UserControl
{
    public DisappearingMessagesViewModel ViewModel { get; }

    public DisappearingMessagesControl()
    {
        this.InitializeComponent();
        ViewModel = new DisappearingMessagesViewModel();
    }

    private void EnableToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // Additional logic if needed when toggle changes
    }
}
