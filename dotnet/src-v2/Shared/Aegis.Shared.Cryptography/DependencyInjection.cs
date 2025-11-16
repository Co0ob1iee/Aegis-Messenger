using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Cryptography.Services;
using Aegis.Shared.Cryptography.SignalProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Shared.Cryptography;

/// <summary>
/// Dependency injection configuration for Shared.Cryptography
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add cryptography services
    /// </summary>
    public static IServiceCollection AddCryptographyServices(this IServiceCollection services)
    {
        // Core encryption services
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddSingleton<IKeyDerivationService, Pbkdf2KeyDerivationService>();

        // Signal Protocol services
        services.AddSingleton<ISignalProtocol, SignalProtocolService>();

        // Privacy protection services
        services.AddSingleton<IMessagePaddingService, MessagePaddingService>();
        services.AddSingleton<ITimestampFuzzingService, TimestampFuzzingService>();

        // Note: IKeyStore needs platform-specific implementation
        // Register in platform-specific projects (e.g., Windows DPAPI, Android KeyStore)

        return services;
    }
}
