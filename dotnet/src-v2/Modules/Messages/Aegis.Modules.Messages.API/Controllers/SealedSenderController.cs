using Aegis.Shared.Cryptography.SealedSender;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Modules.Messages.API.Controllers;

/// <summary>
/// API endpoints for sealed sender (anonymous messaging) certificate management
/// </summary>
[ApiController]
[Route("api/sealed-sender")]
[Authorize]
public class SealedSenderController : ControllerBase
{
    private readonly ISenderCertificateService _certificateService;
    private readonly ILogger<SealedSenderController> _logger;

    public SealedSenderController(
        ISenderCertificateService certificateService,
        ILogger<SealedSenderController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    /// <summary>
    /// Request a sender certificate for sending sealed sender messages
    /// Certificate is valid for 24 hours by default
    /// </summary>
    /// <param name="request">Certificate request</param>
    /// <returns>Signed sender certificate</returns>
    [HttpPost("certificate")]
    [ProducesResponseType(typeof(SenderCertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SenderCertificateDto>> RequestCertificate(
        [FromBody] RequestSenderCertificateRequest request)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user ID");
            }

            _logger.LogInformation(
                "User {UserId} requested sender certificate for device {DeviceId}",
                userId, request.DeviceId);

            // Generate or get cached certificate
            var certificate = await _certificateService.GetOrCreateCertificateAsync(
                userId,
                request.DeviceId,
                request.IdentityKey);

            var dto = new SenderCertificateDto(
                certificate.CertificateId,
                certificate.SenderId,
                certificate.DeviceId,
                certificate.SenderIdentityKey,
                certificate.ExpiresAt,
                certificate.IssuedAt,
                certificate.ServerSignature
            );

            _logger.LogInformation(
                "Issued sender certificate {CertificateId} for user {UserId}, expires at {ExpiresAt}",
                certificate.CertificateId, userId, certificate.ExpiresAt);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to issue sender certificate");
            return StatusCode(500, "Failed to issue sender certificate");
        }
    }

    /// <summary>
    /// Verify a sender certificate
    /// </summary>
    /// <param name="request">Verification request</param>
    /// <returns>Verification result</returns>
    [HttpPost("certificate/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyCertificateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VerifyCertificateResponse>> VerifyCertificate(
        [FromBody] VerifySenderCertificateRequest request)
    {
        try
        {
            var certificate = new SenderCertificate
            {
                CertificateId = request.CertificateId,
                SenderId = request.SenderId,
                DeviceId = request.DeviceId,
                SenderIdentityKey = request.SenderIdentityKey,
                ExpiresAt = request.ExpiresAt,
                IssuedAt = request.IssuedAt,
                ServerSignature = request.ServerSignature
            };

            var isValid = await _certificateService.VerifyCertificateAsync(
                certificate,
                request.ServerPublicKey);

            _logger.LogDebug(
                "Certificate {CertificateId} verification result: {IsValid}",
                request.CertificateId, isValid);

            return Ok(new VerifyCertificateResponse
            {
                IsValid = isValid,
                IsExpired = certificate.IsExpired(),
                ExpiresAt = certificate.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify certificate");
            return StatusCode(500, "Failed to verify certificate");
        }
    }

    /// <summary>
    /// Get server's public key for certificate verification
    /// This key is used to verify the signature on sender certificates
    /// </summary>
    /// <returns>Server's public key</returns>
    [HttpGet("server-key")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServerPublicKeyResponse), StatusCodes.Status200OK)]
    public ActionResult<ServerPublicKeyResponse> GetServerPublicKey()
    {
        try
        {
            // Get server public key from certificate service
            var certificateService = _certificateService as SenderCertificateService;
            if (certificateService == null)
            {
                return StatusCode(500, "Certificate service not available");
            }

            var publicKey = certificateService.GetServerPublicKey();

            return Ok(new ServerPublicKeyResponse
            {
                PublicKey = publicKey,
                Algorithm = "RSA-2048",
                Usage = "Certificate signing and verification"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server public key");
            return StatusCode(500, "Failed to get server public key");
        }
    }

    /// <summary>
    /// Revoke a sender certificate (e.g., when user's identity key changes)
    /// </summary>
    /// <param name="certificateId">Certificate ID to revoke</param>
    /// <returns>Success status</returns>
    [HttpDelete("certificate/{certificateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeCertificate(Guid certificateId)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid user ID");
            }

            _logger.LogInformation(
                "User {UserId} revoking certificate {CertificateId}",
                userId, certificateId);

            await _certificateService.RevokeCertificateAsync(certificateId);

            _logger.LogInformation(
                "Certificate {CertificateId} revoked successfully",
                certificateId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke certificate {CertificateId}", certificateId);
            return StatusCode(500, "Failed to revoke certificate");
        }
    }
}

#region Request/Response Models

/// <summary>
/// Request for sender certificate
/// </summary>
public record RequestSenderCertificateRequest
{
    /// <summary>
    /// Device ID (default: 1)
    /// </summary>
    public uint DeviceId { get; init; } = 1;

    /// <summary>
    /// User's public identity key
    /// </summary>
    public required string IdentityKey { get; init; }
}

/// <summary>
/// Request to verify sender certificate
/// </summary>
public record VerifySenderCertificateRequest
{
    public required Guid CertificateId { get; init; }
    public required Guid SenderId { get; init; }
    public required uint DeviceId { get; init; }
    public required string SenderIdentityKey { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required DateTime IssuedAt { get; init; }
    public required string ServerSignature { get; init; }
    public required string ServerPublicKey { get; init; }
}

/// <summary>
/// Response for certificate verification
/// </summary>
public record VerifyCertificateResponse
{
    public required bool IsValid { get; init; }
    public required bool IsExpired { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Response containing server's public key
/// </summary>
public record ServerPublicKeyResponse
{
    public required string PublicKey { get; init; }
    public required string Algorithm { get; init; }
    public required string Usage { get; init; }
}

#endregion
