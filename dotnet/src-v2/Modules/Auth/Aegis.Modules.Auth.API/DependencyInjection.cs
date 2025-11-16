using System.Text;
using Aegis.Modules.Auth.API.Services;
using Aegis.Modules.Auth.Application;
using Aegis.Modules.Auth.Application.Abstractions;
using Aegis.Modules.Auth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Aegis.Modules.Auth.API;

/// <summary>
/// Dependency injection configuration for Auth API layer
/// Aggregates all Auth module dependencies
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add all layers
        services.AddAuthApplication();
        services.AddAuthInfrastructure(configuration);

        // JWT Service
        services.AddScoped<IJwtService, JwtService>();

        // JWT Authentication
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "Aegis.Messenger",
                ValidAudience = configuration["Jwt:Audience"] ?? "Aegis.Messenger.Client",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
