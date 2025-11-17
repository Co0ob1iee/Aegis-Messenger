using System;

namespace Aegis.Desktop.Models;

public class Message
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool IsOwn { get; set; }
    public bool IsRead { get; set; }

    // Disappearing messages
    public int? DisappearAfterSeconds { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    // Sealed sender
    public bool IsSealedSender { get; set; }
}
