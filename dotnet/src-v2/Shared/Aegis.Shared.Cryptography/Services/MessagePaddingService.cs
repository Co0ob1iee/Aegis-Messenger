using System.Security.Cryptography;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.Services;

/// <summary>
/// Message padding service to hide message length
/// Prevents traffic analysis attacks
/// </summary>
public class MessagePaddingService : IMessagePaddingService
{
    private readonly ILogger<MessagePaddingService> _logger;

    // Padding sizes - powers of 2 for efficient allocation
    private static readonly int[] PaddingSizes =
    {
        256,    // ~256 bytes  - short messages
        512,    // ~512 bytes  - medium messages
        1024,   // ~1 KB       - longer messages
        2048,   // ~2 KB
        4096,   // ~4 KB
        8192,   // ~8 KB       - very long messages
        16384,  // ~16 KB
        32768,  // ~32 KB
        65536   // ~64 KB      - maximum
    };

    public MessagePaddingService(ILogger<MessagePaddingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Pad message to hide its true length
    /// Format: [2 bytes: original length][plaintext][random padding]
    /// </summary>
    public byte[] PadMessage(byte[] plaintext)
    {
        if (plaintext == null || plaintext.Length == 0)
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));

        // Find target size (smallest padding size that fits plaintext + length header)
        var targetSize = PaddingSizes.FirstOrDefault(s => s >= plaintext.Length + 2);

        if (targetSize == 0)
        {
            _logger.LogWarning(
                "Message too large for padding ({Size} bytes), using plaintext + 2KB padding",
                plaintext.Length);

            // For very large messages, just add 2KB of padding
            targetSize = plaintext.Length + 2048;
        }

        var padded = new byte[targetSize];

        // Write original length (2 bytes, little-endian)
        if (plaintext.Length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"Message too large: {plaintext.Length} bytes (max: {ushort.MaxValue})",
                nameof(plaintext));
        }

        Buffer.BlockCopy(BitConverter.GetBytes((ushort)plaintext.Length), 0, padded, 0, 2);

        // Write plaintext
        Buffer.BlockCopy(plaintext, 0, padded, 2, plaintext.Length);

        // Fill rest with random padding
        var paddingStart = 2 + plaintext.Length;
        var paddingLength = targetSize - paddingStart;

        if (paddingLength > 0)
        {
            using var rng = RandomNumberGenerator.Create();
            var padding = new byte[paddingLength];
            rng.GetBytes(padding);
            Buffer.BlockCopy(padding, 0, padded, paddingStart, paddingLength);
        }

        _logger.LogDebug(
            "Padded message: {Original} -> {Padded} bytes ({Overhead}% overhead)",
            plaintext.Length,
            targetSize,
            (int)((targetSize - plaintext.Length) * 100.0 / plaintext.Length));

        return padded;
    }

    /// <summary>
    /// Unpad message to retrieve original plaintext
    /// </summary>
    public byte[] UnpadMessage(byte[] padded)
    {
        if (padded == null || padded.Length < 2)
            throw new ArgumentException("Padded message must be at least 2 bytes", nameof(padded));

        // Read original length (2 bytes, little-endian)
        var originalLength = BitConverter.ToUInt16(padded, 0);

        if (originalLength + 2 > padded.Length)
        {
            throw new ArgumentException(
                $"Invalid padded message: claimed length {originalLength} but total size is {padded.Length}",
                nameof(padded));
        }

        // Extract plaintext
        var plaintext = new byte[originalLength];
        Buffer.BlockCopy(padded, 2, plaintext, 0, originalLength);

        return plaintext;
    }

    /// <summary>
    /// Get the padded size for a given plaintext length
    /// Useful for bandwidth estimation
    /// </summary>
    public int GetPaddedSize(int plaintextLength)
    {
        return PaddingSizes.FirstOrDefault(s => s >= plaintextLength + 2)
            ?? plaintextLength + 2048;
    }

    /// <summary>
    /// Calculate padding overhead percentage
    /// </summary>
    public double CalculateOverhead(int plaintextLength)
    {
        var paddedSize = GetPaddedSize(plaintextLength);
        return (paddedSize - plaintextLength) * 100.0 / plaintextLength;
    }
}
