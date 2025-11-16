using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Aegis.Backend.Services;

/// <summary>
/// JWT token generation and validation service
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    public string GenerateToken(Guid userId, string username)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "AegisMessenger";
        var audience = _configuration["Jwt:Audience"] ?? "AegisMessengerClients";
        var expiryHours = int.Parse(_configuration["Jwt:ExpiryHours"] ?? "24");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "AegisMessenger";
        var audience = _configuration["Jwt:Audience"] ?? "AegisMessengerClients";

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
