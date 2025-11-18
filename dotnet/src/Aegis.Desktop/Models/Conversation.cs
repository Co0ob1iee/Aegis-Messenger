using System;
using System.Collections.Generic;

namespace Aegis.Desktop.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }

    // Disappearing messages settings
    public int? DefaultDisappearAfterSeconds { get; set; }
    public bool DisappearingMessagesEnabled { get; set; }

    // Privacy settings
    public bool SealedSenderEnabled { get; set; }
    public bool TypingIndicatorsEnabled { get; set; }
    public bool ReadReceiptsEnabled { get; set; }

    public List<string> ParticipantIds { get; set; } = new();
}
