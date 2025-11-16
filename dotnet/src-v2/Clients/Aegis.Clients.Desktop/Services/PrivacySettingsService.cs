using Aegis.Clients.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aegis.Clients.Desktop.Services;

/// <summary>
/// Service for persisting and loading privacy settings
/// Stores settings in local app data as JSON
/// </summary>
public class PrivacySettingsService : IPrivacySettingsService
{
    private readonly ILogger<PrivacySettingsService> _logger;
    private readonly string _settingsFilePath;
    private PrivacySettings? _cachedSettings;

    public PrivacySettingsService(ILogger<PrivacySettingsService> logger)
    {
        _logger = logger;

        // Store settings in %LOCALAPPDATA%\AegisMessenger\privacy-settings.json
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var aegisFolder = Path.Combine(appDataPath, "AegisMessenger");
        Directory.CreateDirectory(aegisFolder);

        _settingsFilePath = Path.Combine(aegisFolder, "privacy-settings.json");
    }

    public PrivacySettings GetSettings()
    {
        try
        {
            // Return cached settings if available
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            // Load from file if exists
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _cachedSettings = JsonSerializer.Deserialize<PrivacySettings>(json);

                if (_cachedSettings != null)
                {
                    _logger.LogInformation("Privacy settings loaded from {Path}", _settingsFilePath);
                    return _cachedSettings;
                }
            }

            // Return default settings if no file exists
            _cachedSettings = GetDefaultSettings();
            _logger.LogInformation("Using default privacy settings");
            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load privacy settings, using defaults");
            return GetDefaultSettings();
        }
    }

    public async Task SaveSettingsAsync(PrivacySettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            // Update cache
            _cachedSettings = settings;

            _logger.LogInformation("Privacy settings saved to {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save privacy settings");
            throw;
        }
    }

    private PrivacySettings GetDefaultSettings()
    {
        return new PrivacySettings
        {
            // Message Privacy - disabled by default (user opt-in)
            MessagePaddingEnabled = false,
            PaddingStrategy = PaddingStrategy.Random,

            TimestampFuzzingEnabled = false,
            TimestampFuzzingRangeSeconds = 60,

            SealedSenderEnabled = true, // Enable by default for better privacy

            // Message Retention - disabled by default
            DisappearingMessagesEnabled = false,
            DefaultDisappearingMessageTimer = TimeSpan.FromHours(1),

            // Metadata Privacy - enabled by default (standard messenger behavior)
            ReadReceiptsEnabled = true,
            TypingIndicatorsEnabled = true
        };
    }
}
