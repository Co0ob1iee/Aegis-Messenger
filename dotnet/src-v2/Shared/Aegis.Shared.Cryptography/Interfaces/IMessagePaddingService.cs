namespace Aegis.Shared.Cryptography.Interfaces;

/// <summary>
/// Service for padding messages to hide their true length
/// Prevents traffic analysis attacks
/// </summary>
public interface IMessagePaddingService
{
    /// <summary>
    /// Pad message to hide its true length
    /// </summary>
    /// <param name="plaintext">Original message bytes</param>
    /// <returns>Padded message bytes</returns>
    byte[] PadMessage(byte[] plaintext);

    /// <summary>
    /// Unpad message to retrieve original plaintext
    /// </summary>
    /// <param name="padded">Padded message bytes</param>
    /// <returns>Original message bytes</returns>
    byte[] UnpadMessage(byte[] padded);

    /// <summary>
    /// Get the padded size for a given plaintext length
    /// </summary>
    /// <param name="plaintextLength">Original plaintext length</param>
    /// <returns>Padded size in bytes</returns>
    int GetPaddedSize(int plaintextLength);

    /// <summary>
    /// Calculate padding overhead percentage
    /// </summary>
    /// <param name="plaintextLength">Original plaintext length</param>
    /// <returns>Overhead percentage</returns>
    double CalculateOverhead(int plaintextLength);
}
