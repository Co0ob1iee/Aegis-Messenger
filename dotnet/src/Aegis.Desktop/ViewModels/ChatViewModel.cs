using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aegis.Desktop.Models;
using CommunityToolkit.Mvvm.Input;

namespace Aegis.Desktop.ViewModels;

public class ChatViewModel : INotifyPropertyChanged
{
    private string _messageText = string.Empty;
    private Conversation? _currentConversation;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChatViewModel()
    {
        Messages = new ObservableCollection<Message>();
        SendMessageCommand = new RelayCommand(SendMessage, CanSendMessage);

        LoadSampleMessages();
    }

    public ObservableCollection<Message> Messages { get; }

    public string MessageText
    {
        get => _messageText;
        set
        {
            if (_messageText != value)
            {
                _messageText = value;
                OnPropertyChanged();
                (SendMessageCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public Conversation? CurrentConversation
    {
        get => _currentConversation;
        set
        {
            if (_currentConversation != value)
            {
                _currentConversation = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SendMessageCommand { get; }

    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = MessageText,
            Timestamp = DateTime.Now,
            IsOwn = true,
            SenderId = "current-user",
            SenderName = "You",
            DisappearAfterSeconds = CurrentConversation?.DefaultDisappearAfterSeconds
        };

        Messages.Add(message);
        MessageText = string.Empty;
    }

    private bool CanSendMessage()
    {
        return !string.IsNullOrWhiteSpace(MessageText);
    }

    private void LoadSampleMessages()
    {
        Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Content = "Hello! This is a test message.",
            Timestamp = DateTime.Now.AddMinutes(-10),
            IsOwn = false,
            SenderId = "user-1",
            SenderName = "Alice"
        });

        Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            Content = "Hi! How are you doing?",
            Timestamp = DateTime.Now.AddMinutes(-5),
            IsOwn = true,
            SenderId = "current-user",
            SenderName = "You"
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
