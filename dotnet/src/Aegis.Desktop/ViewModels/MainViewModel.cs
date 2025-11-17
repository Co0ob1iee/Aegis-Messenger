using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aegis.Desktop.Models;

namespace Aegis.Desktop.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private Conversation? _selectedConversation;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        Conversations = new ObservableCollection<Conversation>();

        // Sample data for testing
        LoadSampleData();
    }

    public ObservableCollection<Conversation> Conversations { get; }

    public Conversation? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            if (_selectedConversation != value)
            {
                _selectedConversation = value;
                OnPropertyChanged();
            }
        }
    }

    private void LoadSampleData()
    {
        Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            DisplayName = "Alice Smith",
            LastMessage = "Hey! How are you?",
            LastMessageTime = DateTime.Now.AddMinutes(-5),
            UnreadCount = 2,
            DisappearingMessagesEnabled = true,
            DefaultDisappearAfterSeconds = 300
        });

        Conversations.Add(new Conversation
        {
            Id = Guid.NewGuid(),
            DisplayName = "Bob Johnson",
            LastMessage = "See you tomorrow!",
            LastMessageTime = DateTime.Now.AddHours(-1),
            UnreadCount = 0
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
