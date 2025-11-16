using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Aegis.Clients.Desktop.ViewModels;

/// <summary>
/// ViewModel for Privacy Settings page
/// Manages all privacy-related settings with two-way data binding
/// </summary>
public partial class PrivacySettingsViewModel : ObservableObject
{
    private readonly ILogger<PrivacySettingsViewModel> _logger;
    private readonly IPrivacySettingsService _privacySettings;

    public PrivacySettingsViewModel(
        ILogger<PrivacySettingsViewModel> logger,
        IPrivacySettingsService privacySettings)
    {
        _logger = logger;
        _privacySettings = privacySettings;

        // Load settings from storage
        LoadSettings();
    }

    #region Message Privacy Settings

    [ObservableProperty]
    private bool _isMessagePaddingEnabled;

    [ObservableProperty]
    private int _paddingStrategyIndex;

    [ObservableProperty]
    private bool _isTimestampFuzzingEnabled;

    [ObservableProperty]
    private int _timestampFuzzingRangeSeconds = 60;

    [ObservableProperty]
    private bool _isSealedSenderEnabled;

    #endregion

    #region Message Retention Settings

    [ObservableProperty]
    private bool _isDisappearingMessagesEnabled;

    [ObservableProperty]
    private int _disappearingMessageTimerIndex = 4; // Default: 1 hour

    #endregion

    #region Metadata Privacy Settings

    [ObservableProperty]
    private bool _areReadReceiptsEnabled = true;

    [ObservableProperty]
    private bool _areTypingIndicatorsEnabled = true;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            _logger.LogInformation("Saving privacy settings");

            // Map UI values to service settings
            var settings = new PrivacySettings
            {
                // Message Privacy
                MessagePaddingEnabled = IsMessagePaddingEnabled,
                PaddingStrategy = GetPaddingStrategy(PaddingStrategyIndex),

                TimestampFuzzingEnabled = IsTimestampFuzzingEnabled,
                TimestampFuzzingRangeSeconds = TimestampFuzzingRangeSeconds,

                SealedSenderEnabled = IsSealedSenderEnabled,

                // Message Retention
                DisappearingMessagesEnabled = IsDisappearingMessagesEnabled,
                DefaultDisappearingMessageTimer = GetDisappearingMessageTimer(DisappearingMessageTimerIndex),

                // Metadata Privacy
                ReadReceiptsEnabled = AreReadReceiptsEnabled,
                TypingIndicatorsEnabled = AreTypingIndicatorsEnabled
            };

            await _privacySettings.SaveSettingsAsync(settings);

            _logger.LogInformation("Privacy settings saved successfully");

            // Show success notification (implement via messaging service)
            // MessengerService.Send(new SettingsSavedMessage());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save privacy settings");
            // Show error notification
        }
    }

    #endregion

    #region Private Methods

    private void LoadSettings()
    {
        try
        {
            var settings = _privacySettings.GetSettings();

            // Message Privacy
            IsMessagePaddingEnabled = settings.MessagePaddingEnabled;
            PaddingStrategyIndex = GetPaddingStrategyIndex(settings.PaddingStrategy);

            IsTimestampFuzzingEnabled = settings.TimestampFuzzingEnabled;
            TimestampFuzzingRangeSeconds = settings.TimestampFuzzingRangeSeconds;

            IsSealedSenderEnabled = settings.SealedSenderEnabled;

            // Message Retention
            IsDisappearingMessagesEnabled = settings.DisappearingMessagesEnabled;
            DisappearingMessageTimerIndex = GetDisappearingMessageTimerIndex(settings.DefaultDisappearingMessageTimer);

            // Metadata Privacy
            AreReadReceiptsEnabled = settings.ReadReceiptsEnabled;
            AreTypingIndicatorsEnabled = settings.TypingIndicatorsEnabled;

            _logger.LogInformation("Privacy settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load privacy settings");
        }
    }

    private PaddingStrategy GetPaddingStrategy(int index)
    {
        return index switch
        {
            0 => PaddingStrategy.Random,
            1 => PaddingStrategy.FixedBlock,
            2 => PaddingStrategy.Exponential,
            _ => PaddingStrategy.Random
        };
    }

    private int GetPaddingStrategyIndex(PaddingStrategy strategy)
    {
        return strategy switch
        {
            PaddingStrategy.Random => 0,
            PaddingStrategy.FixedBlock => 1,
            PaddingStrategy.Exponential => 2,
            _ => 0
        };
    }

    private TimeSpan GetDisappearingMessageTimer(int index)
    {
        return index switch
        {
            0 => TimeSpan.FromSeconds(30),
            1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(30),
            4 => TimeSpan.FromHours(1),
            5 => TimeSpan.FromHours(6),
            6 => TimeSpan.FromHours(12),
            7 => TimeSpan.FromDays(1),
            8 => TimeSpan.FromDays(7),
            _ => TimeSpan.FromHours(1)
        };
    }

    private int GetDisappearingMessageTimerIndex(TimeSpan timer)
    {
        if (timer.TotalSeconds <= 30) return 0;
        if (timer.TotalMinutes <= 1) return 1;
        if (timer.TotalMinutes <= 5) return 2;
        if (timer.TotalMinutes <= 30) return 3;
        if (timer.TotalHours <= 1) return 4;
        if (timer.TotalHours <= 6) return 5;
        if (timer.TotalHours <= 12) return 6;
        if (timer.TotalDays <= 1) return 7;
        return 8; // 1 week
    }

    #endregion
}

#region Models and Enums

/// <summary>
/// Privacy settings model
/// </summary>
public class PrivacySettings
{
    // Message Privacy
    public bool MessagePaddingEnabled { get; set; }
    public PaddingStrategy PaddingStrategy { get; set; }

    public bool TimestampFuzzingEnabled { get; set; }
    public int TimestampFuzzingRangeSeconds { get; set; } = 60;

    public bool SealedSenderEnabled { get; set; }

    // Message Retention
    public bool DisappearingMessagesEnabled { get; set; }
    public TimeSpan DefaultDisappearingMessageTimer { get; set; } = TimeSpan.FromHours(1);

    // Metadata Privacy
    public bool ReadReceiptsEnabled { get; set; } = true;
    public bool TypingIndicatorsEnabled { get; set; } = true;
}

/// <summary>
/// Padding strategy enum (matches MessagePaddingService)
/// </summary>
public enum PaddingStrategy
{
    Random,
    FixedBlock,
    Exponential
}

/// <summary>
/// Service interface for privacy settings persistence
/// </summary>
public interface IPrivacySettingsService
{
    PrivacySettings GetSettings();
    Task SaveSettingsAsync(PrivacySettings settings);
}

#endregion
