using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Aegis.Desktop.ViewModels;
using Aegis.Desktop.Controls;
using Aegis.Desktop.Views;

namespace Aegis.Desktop;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    public ChatViewModel ChatViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        // Get ViewModel from DI
        var app = Application.Current as App;
        ViewModel = app?.Host.Services.GetService(typeof(MainViewModel)) as MainViewModel
            ?? new MainViewModel();
        ChatViewModel = app?.Host.Services.GetService(typeof(ChatViewModel)) as ChatViewModel
            ?? new ChatViewModel();

        Title = "Aegis Messenger - Secure End-to-End Encrypted Messaging";
    }

    private async void DisappearingMessagesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Disappearing Messages",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var control = new DisappearingMessagesControl();

        // Load current settings
        if (ViewModel.SelectedConversation != null)
        {
            control.ViewModel.IsEnabled = ViewModel.SelectedConversation.DisappearingMessagesEnabled;
            if (ViewModel.SelectedConversation.DefaultDisappearAfterSeconds.HasValue)
            {
                var seconds = ViewModel.SelectedConversation.DefaultDisappearAfterSeconds.Value;
                var matchingTimer = control.ViewModel.Timers.FirstOrDefault(t => t.Seconds == seconds);
                if (matchingTimer != null)
                {
                    control.ViewModel.SelectedTimer = matchingTimer;
                }
            }
        }

        // Handle settings applied
        control.ViewModel.SettingsApplied += (s, args) =>
        {
            if (ViewModel.SelectedConversation != null)
            {
                ViewModel.SelectedConversation.DisappearingMessagesEnabled = args.Enabled;
                ViewModel.SelectedConversation.DefaultDisappearAfterSeconds = args.DisappearAfterSeconds;

                // TODO: Call API to save settings
                // await apiService.SetDisappearingMessagesAsync(ViewModel.SelectedConversation.Id, args.DisappearAfterSeconds);
            }

            _ = dialog.Hide();
        };

        dialog.Content = control;
        await dialog.ShowAsync();
    }

    private async void PrivacySettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Privacy Settings",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var privacyPage = new PrivacySettingsPage();

        // Load current settings
        if (ViewModel.SelectedConversation != null)
        {
            privacyPage.ViewModel.SealedSenderEnabled = ViewModel.SelectedConversation.SealedSenderEnabled;
            privacyPage.ViewModel.TypingIndicatorsEnabled = ViewModel.SelectedConversation.TypingIndicatorsEnabled;
            privacyPage.ViewModel.ReadReceiptsEnabled = ViewModel.SelectedConversation.ReadReceiptsEnabled;
        }

        // Handle settings saved
        privacyPage.ViewModel.SettingsSaved += (s, args) =>
        {
            if (ViewModel.SelectedConversation != null)
            {
                ViewModel.SelectedConversation.SealedSenderEnabled = args.SealedSenderEnabled;
                ViewModel.SelectedConversation.TypingIndicatorsEnabled = args.TypingIndicatorsEnabled;
                ViewModel.SelectedConversation.ReadReceiptsEnabled = args.ReadReceiptsEnabled;

                // TODO: Call API to save settings
                // await apiService.UpdatePrivacySettingsAsync(ViewModel.SelectedConversation.Id, settings);
            }

            _ = dialog.Hide();
        };

        dialog.Content = privacyPage;
        await dialog.ShowAsync();
    }
}
