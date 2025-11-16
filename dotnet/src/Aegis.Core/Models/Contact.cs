using System;

namespace Aegis.Core.Models;

/// <summary>
/// Represents a contact in the user's contact list
/// </summary>
public class Contact
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Owner user ID (who owns this contact)
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Contact user ID (the actual contact)
    /// </summary>
    public Guid ContactUserId { get; set; }

    /// <summary>
    /// Custom nickname for this contact (optional)
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// When this contact was added
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Flag indicating if this contact is blocked
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Flag indicating if this contact is a favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Safety number verification status
    /// </summary>
    public SafetyNumberStatus SafetyNumberStatus { get; set; } = SafetyNumberStatus.Unverified;

    /// <summary>
    /// Safety number (fingerprint of identity keys)
    /// Used to verify identity and detect MITM attacks
    /// </summary>
    public string? SafetyNumber { get; set; }

    /// <summary>
    /// Timestamp when safety number was last verified
    /// </summary>
    public DateTime? SafetyNumberVerifiedAt { get; set; }
}

/// <summary>
/// Safety number verification status
/// </summary>
public enum SafetyNumberStatus
{
    /// <summary>
    /// Safety number not verified
    /// </summary>
    Unverified = 1,

    /// <summary>
    /// Safety number verified
    /// </summary>
    Verified = 2,

    /// <summary>
    /// Safety number changed (potential MITM warning)
    /// </summary>
    Changed = 3
}

/// <summary>
/// File attachment metadata
/// </summary>
public class FileAttachment
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type
    /// </summary>
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Encrypted file content (stored as blob or reference to storage)
    /// </summary>
    public byte[]? EncryptedContent { get; set; }

    /// <summary>
    /// URL to encrypted file (if stored externally)
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Encryption key for the file (encrypted with session key)
    /// Base64 encoded
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// IV (Initialization Vector) for AES encryption
    /// Base64 encoded
    /// </summary>
    public string? IV { get; set; }

    /// <summary>
    /// SHA-256 hash of the original file (for integrity verification)
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public Guid UploaderId { get; set; }

    /// <summary>
    /// Flag indicating if file is an image
    /// </summary>
    public bool IsImage { get; set; }

    /// <summary>
    /// Thumbnail (for images, Base64 encoded)
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Expiration time (for disappearing files)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
