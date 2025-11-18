using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aegis.Shared.Cryptography.SealedSender;

/// <summary>
/// Sender certificate issued by the server to enable sealed sender (anonymous sender) messages.
/// The certificate proves to the recipient that the sender is authorized without revealing the sender to the server.
/// </summary>
public class SenderCertificate
{
    /// <summary>
    /// Unique identifier for this certificate
    /// </summary>
    public Guid CertificateId { get; set; }

    /// <summary>
    /// User ID of the sender
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Device ID of the sender (default: 1)
    /// </summary>
    public uint DeviceId { get; set; }

    /// <summary>
    /// Sender's public identity key (for verification)
    /// </summary>
    public string SenderIdentityKey { get; set; } = string.Empty;

    /// <summary>
    /// Expiration timestamp (certificates are time-limited for security)
    /// Typically valid for 24-48 hours
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Certificate issuance timestamp
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Server signature of the certificate (proves authenticity)
    /// Signs: SenderId + DeviceId + SenderIdentityKey + ExpiresAt
    /// </summary>
    public string ServerSignature { get; set; } = string.Empty;

    /// <summary>
    /// Serialize certificate to bytes for signing/verification
    /// </summary>
    public byte[] Serialize()
    {
        var data = new
        {
            SenderId = SenderId.ToString(),
            DeviceId,
            SenderIdentityKey,
            ExpiresAt = ExpiresAt.ToString("O")
        };

        var json = JsonSerializer.Serialize(data);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Check if certificate is expired
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Verify server signature on the certificate
    /// </summary>
    public bool VerifySignature(string serverPublicKey)
    {
        try
        {
            var data = Serialize();
            var signatureBytes = Convert.FromBase64String(ServerSignature);
            var publicKeyBytes = Convert.FromBase64String(serverPublicKey);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            return rsa.VerifyData(
                data,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// DTO for sender certificate
/// </summary>
public record SenderCertificateDto(
    Guid CertificateId,
    Guid SenderId,
    uint DeviceId,
    string SenderIdentityKey,
    DateTime ExpiresAt,
    DateTime IssuedAt,
    string ServerSignature
);
