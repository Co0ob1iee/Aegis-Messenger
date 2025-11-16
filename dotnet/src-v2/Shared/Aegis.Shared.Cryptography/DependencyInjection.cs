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
    /// Automatically selects the most secure storage available for the current platform:
    /// - Windows: DPAPI (Data Protection API)
    /// - Android: Android KeyStore System (hardware-backed when available)
    /// - Linux: Linux KeyRing (libsecret/KWallet)
    /// - Fallback: In-Memory (development only)
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
        else if (OperatingSystem.IsAndroid())
        {
            // Android: Use Android KeyStore System
            // NOTE: Current implementation is simplified for cross-platform development
            // For production, use AndroidX.Security.Crypto - see ANDROID_KEYSTORE_PRODUCTION.md
            services.AddSingleton<IKeyStore>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AndroidKeyStore>>();
                return new AndroidKeyStore(logger);
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: Use Linux KeyRing (libsecret/KWallet/Secret Service API)
            // NOTE: Current implementation is file-based fallback
            // For production, use libsecret or D-Bus Secret Service API - see LINUX_KEYRING_PRODUCTION.md
            services.AddSingleton<IKeyStore>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LinuxKeyRingStore>>();
                return new LinuxKeyRingStore(logger);
            });
        }
        else
        {
            // Other platforms: Fall back to in-memory storage
            // WARNING: In-memory storage is NOT SECURE and should only be used for development
            services.AddSingleton<IKeyStore>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<InMemoryKeyStore>>();
                logger.LogWarning(
                    "Using InMemoryKeyStore - NOT SECURE FOR PRODUCTION. " +
                    "Keys are not encrypted and will be lost on application restart.");
                return new InMemoryKeyStore();
            });
        }

        return services;
    }
}
