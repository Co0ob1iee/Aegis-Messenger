using Aegis.Backend.Services;
using Aegis.Core.Cryptography;
using Aegis.Data.Context;
using Aegis.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AegisDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AegisDbContext context,
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new { error = "Username already exists" });
        }

        // Hash password
        var (hash, salt) = KeyDerivation.HashPassword(request.Password);

        // Create user
        var user = new UserEntity
        {
            Username = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName ?? request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            RegistrationId = (uint)Random.Shared.Next(1, int.MaxValue),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Id, user.Username);

        _logger.LogInformation("New user registered: {Username}", user.Username);

        return Ok(new
        {
            userId = user.Id,
            username = user.Username,
            token
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Find user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        // Verify password
        if (!KeyDerivation.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }

        // Update last seen
        user.LastSeenAt = DateTime.UtcNow;
        user.IsOnline = true;
        await _context.SaveChangesAsync();

        // Generate token
        var token = _jwtService.GenerateToken(user.Id, user.Username);

        _logger.LogInformation("User logged in: {Username}", user.Username);

        return Ok(new
        {
            userId = user.Id,
            username = user.Username,
            displayName = user.DisplayName,
            token
        });
    }
}

public record RegisterRequest(string Username, string Password, string? Email, string? DisplayName);
public record LoginRequest(string Username, string Password);
