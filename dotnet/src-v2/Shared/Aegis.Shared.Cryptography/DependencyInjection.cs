using System.Runtime.InteropServices;
using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Cryptography.Services;
using Aegis.Shared.Cryptography.SignalProtocol;
using Aegis.Shared.Cryptography.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Platform-specific key storage
        services.AddPlatformSpecificKeyStore();

        return services;
    }

    /// <summary>
    /// Add platform-specific key store implementation
    /// </summary>
    private static IServiceCollection AddPlatformSpecificKeyStore(this IServiceCollection services)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Use DPAPI for secure key storage
            services.AddSingleton<IKeyStore>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WindowsDpapiKeyStore>>();
                return new WindowsDpapiKeyStore(logger);
            });
        }
        else
        {
            // Other platforms: Fall back to in-memory storage
            // TODO: Implement Android KeyStore, Linux KeyRing, etc.
            services.AddSingleton<IKeyStore, InMemoryKeyStore>();
        }

        return services;
    }
}
