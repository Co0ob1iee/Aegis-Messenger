using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.SealedSender;

/// <summary>
/// Service for managing sender certificates
/// Certificates prove that a sender is authorized to send sealed sender messages
/// </summary>
public class SenderCertificateService : ISenderCertificateService
{
    private readonly ILogger<SenderCertificateService> _logger;
    private readonly RSA _serverKey;
    private readonly string _serverPublicKey;

    // Certificate cache: userId -> certificate
    private readonly ConcurrentDictionary<Guid, SenderCertificate> _certificateCache;

    // Revoked certificates
    private readonly ConcurrentDictionary<Guid, DateTime> _revokedCertificates;

    public SenderCertificateService(ILogger<SenderCertificateService> logger)
    {
        _logger = logger;
        _certificateCache = new ConcurrentDictionary<Guid, SenderCertificate>();
        _revokedCertificates = new ConcurrentDictionary<Guid, DateTime>();

        // Generate server key pair (in production, this should be loaded from secure storage)
        _serverKey = RSA.Create(2048);
        _serverPublicKey = Convert.ToBase64String(_serverKey.ExportSubjectPublicKeyInfo());

        _logger.LogInformation("SenderCertificateService initialized with server public key");
    }

    /// <inheritdoc/>
    public async Task<SenderCertificate> GenerateCertificateAsync(
        Guid userId,
        uint deviceId,
        string identityKey,
        TimeSpan? validityPeriod = null)
    {
        try
        {
            var validity = validityPeriod ?? TimeSpan.FromHours(24);
            var now = DateTime.UtcNow;

            var certificate = new SenderCertificate
            {
                CertificateId = Guid.NewGuid(),
                SenderId = userId,
                DeviceId = deviceId,
                SenderIdentityKey = identityKey,
                IssuedAt = now,
                ExpiresAt = now.Add(validity)
            };

            // Sign the certificate with server's private key
            var dataToSign = certificate.Serialize();
            var signature = _serverKey.SignData(
                dataToSign,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            certificate.ServerSignature = Convert.ToBase64String(signature);

            // Cache the certificate
            _certificateCache[userId] = certificate;

            _logger.LogInformation(
                "Generated sender certificate for user {UserId}, valid until {ExpiresAt}",
                userId, certificate.ExpiresAt);

            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate sender certificate for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyCertificateAsync(SenderCertificate certificate, string serverPublicKey)
    {
        try
        {
            // Check if revoked
            if (_revokedCertificates.ContainsKey(certificate.CertificateId))
            {
                _logger.LogWarning(
                    "Certificate {CertificateId} has been revoked",
                    certificate.CertificateId);
                return false;
            }

            // Check expiration
            if (certificate.IsExpired())
            {
                _logger.LogWarning(
                    "Certificate {CertificateId} has expired (expired at {ExpiresAt})",
                    certificate.CertificateId, certificate.ExpiresAt);
                return false;
            }

            // Verify signature
            if (!certificate.VerifySignature(serverPublicKey))
            {
                _logger.LogWarning(
                    "Certificate {CertificateId} has invalid signature",
                    certificate.CertificateId);
                return false;
            }

            _logger.LogDebug(
                "Certificate {CertificateId} verified successfully",
                certificate.CertificateId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify certificate {CertificateId}", certificate.CertificateId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<SenderCertificate> GetOrCreateCertificateAsync(
        Guid userId,
        uint deviceId,
        string identityKey)
    {
        try
        {
            // Check cache for existing valid certificate
            if (_certificateCache.TryGetValue(userId, out var cachedCert))
            {
                if (!cachedCert.IsExpired() &&
                    !_revokedCertificates.ContainsKey(cachedCert.CertificateId))
                {
                    _logger.LogDebug("Using cached certificate for user {UserId}", userId);
                    return cachedCert;
                }

                // Remove expired/revoked certificate from cache
                _certificateCache.TryRemove(userId, out _);
            }

            // Generate new certificate
            _logger.LogInformation("Generating new certificate for user {UserId}", userId);
            return await GenerateCertificateAsync(userId, deviceId, identityKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create certificate for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RevokeCertificateAsync(Guid certificateId)
    {
        try
        {
            _revokedCertificates[certificateId] = DateTime.UtcNow;

            // Remove from cache if present
            var certToRemove = _certificateCache
                .FirstOrDefault(kvp => kvp.Value.CertificateId == certificateId);

            if (certToRemove.Key != Guid.Empty)
            {
                _certificateCache.TryRemove(certToRemove.Key, out _);
            }

            _logger.LogInformation("Revoked certificate {CertificateId}", certificateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke certificate {CertificateId}", certificateId);
            throw;
        }
    }

    /// <summary>
    /// Get server public key (for distribution to clients)
    /// </summary>
    public string GetServerPublicKey() => _serverPublicKey;

    /// <summary>
    /// Cleanup expired certificates from cache (background task)
    /// </summary>
    public void CleanupExpiredCertificates()
    {
        var expiredCount = 0;

        foreach (var kvp in _certificateCache)
        {
            if (kvp.Value.IsExpired())
            {
                _certificateCache.TryRemove(kvp.Key, out _);
                expiredCount++;
            }
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired certificates", expiredCount);
        }
    }
}
