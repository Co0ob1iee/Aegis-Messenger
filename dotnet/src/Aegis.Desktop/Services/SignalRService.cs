using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Aegis.Desktop.Services;

public class SignalRService
{
    private HubConnection? _connection;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<TypingIndicatorEventArgs>? TypingIndicatorReceived;

    public async Task ConnectAsync(string hubUrl, string accessToken)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<Guid, string, DateTime, int?>("ReceiveMessage",
            (messageId, content, timestamp, disappearAfterSeconds) =>
            {
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                {
                    MessageId = messageId,
                    Content = content,
                    Timestamp = timestamp,
                    DisappearAfterSeconds = disappearAfterSeconds
                });
            });

        _connection.On<Guid, bool>("TypingIndicator", (userId, isTyping) =>
        {
            TypingIndicatorReceived?.Invoke(this, new TypingIndicatorEventArgs
            {
                UserId = userId,
                IsTyping = isTyping
            });
        });

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }

    public async Task SendMessageAsync(Guid conversationId, string content, int? disappearAfterSeconds)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessage", conversationId, content, disappearAfterSeconds);
        }
    }
}

public class MessageReceivedEventArgs : EventArgs
{
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int? DisappearAfterSeconds { get; set; }
}

public class TypingIndicatorEventArgs : EventArgs
{
    public Guid UserId { get; set; }
    public bool IsTyping { get; set; }
}
