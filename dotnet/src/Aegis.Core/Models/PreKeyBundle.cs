using System;

namespace Aegis.Core.Models;

/// <summary>
/// Pre-Key Bundle used for Signal Protocol's X3DH key agreement
/// Contains all public keys needed to initiate an encrypted session
/// </summary>
public class PreKeyBundle
{
    /// <summary>
    /// User's registration ID (unique identifier for the device)
    /// </summary>
    public uint RegistrationId { get; set; }

    /// <summary>
    /// Device ID (for multi-device support)
    /// </summary>
    public uint DeviceId { get; set; } = 1;

    /// <summary>
    /// Pre-key ID (identifier for this specific pre-key)
    /// </summary>
    public uint PreKeyId { get; set; }

    /// <summary>
    /// Public part of the pre-key (Base64 encoded)
    /// </summary>
    public string PreKeyPublic { get; set; } = string.Empty;

    /// <summary>
    /// Signed pre-key ID
    /// </summary>
    public uint SignedPreKeyId { get; set; }

    /// <summary>
    /// Public part of the signed pre-key (Base64 encoded)
    /// </summary>
    public string SignedPreKeyPublic { get; set; } = string.Empty;

    /// <summary>
    /// Signature of the signed pre-key (signed with identity key)
    /// Base64 encoded
    /// </summary>
    public string SignedPreKeySignature { get; set; } = string.Empty;

    /// <summary>
    /// Long-term identity key (public part, Base64 encoded)
    /// </summary>
    public string IdentityKey { get; set; } = string.Empty;

    /// <summary>
    /// User ID who owns this pre-key bundle
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Timestamp when this bundle was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Flag indicating if this bundle has been used
    /// One-time pre-keys should be deleted after use
    /// </summary>
    public bool IsUsed { get; set; }
}

/// <summary>
/// Stored pre-key (private part kept locally)
/// </summary>
public class StoredPreKey
{
    /// <summary>
    /// Pre-key ID
    /// </summary>
    public uint KeyId { get; set; }

    /// <summary>
    /// Private key (encrypted with master key)
    /// Base64 encoded
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Public key (Base64 encoded)
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stored signed pre-key (with signature)
/// </summary>
public class StoredSignedPreKey : StoredPreKey
{
    /// <summary>
    /// Signature of the public key (signed with identity key)
    /// Base64 encoded
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the signature
    /// </summary>
    public DateTime SignatureTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Identity key pair (long-term key)
/// </summary>
public class IdentityKeyPair
{
    /// <summary>
    /// Private key (encrypted, Base64 encoded)
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Public key (Base64 encoded)
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// User ID who owns this key pair
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
