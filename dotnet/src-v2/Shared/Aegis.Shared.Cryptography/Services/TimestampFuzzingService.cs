using System.Security.Cryptography;
using Aegis.Shared.Cryptography.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aegis.Shared.Cryptography.Services;

/// <summary>
/// Timestamp fuzzing service to prevent timing correlation attacks
/// Reduces precision of timestamps to protect user privacy
/// </summary>
public class TimestampFuzzingService : ITimestampFuzzingService
{
    private readonly ILogger<TimestampFuzzingService> _logger;

    public TimestampFuzzingService(ILogger<TimestampFuzzingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Fuzz timestamp to nearest minute
    /// Removes seconds and milliseconds
    /// </summary>
    public DateTime FuzzToMinute(DateTime timestamp)
    {
        var fuzzed = new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            timestamp.Minute,
            0,  // Seconds = 0
            0,  // Milliseconds = 0
            timestamp.Kind);

        _logger.LogTrace(
            "Fuzzed timestamp to minute: {Original} -> {Fuzzed}",
            timestamp,
            fuzzed);

        return fuzzed;
    }

    /// <summary>
    /// Fuzz timestamp to nearest 5 minutes
    /// Provides more privacy at cost of precision
    /// </summary>
    public DateTime FuzzTo5Minutes(DateTime timestamp)
    {
        var minute = (timestamp.Minute / 5) * 5;

        var fuzzed = new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            minute,
            0,
            0,
            timestamp.Kind);

        _logger.LogTrace(
            "Fuzzed timestamp to 5 minutes: {Original} -> {Fuzzed}",
            timestamp,
            fuzzed);

        return fuzzed;
    }

    /// <summary>
    /// Fuzz timestamp to nearest 15 minutes
    /// Maximum privacy for timestamps
    /// </summary>
    public DateTime FuzzTo15Minutes(DateTime timestamp)
    {
        var minute = (timestamp.Minute / 15) * 15;

        var fuzzed = new DateTime(
            timestamp.Year,
            timestamp.Month,
            timestamp.Day,
            timestamp.Hour,
            minute,
            0,
            0,
            timestamp.Kind);

        _logger.LogTrace(
            "Fuzzed timestamp to 15 minutes: {Original} -> {Fuzzed}",
            timestamp,
            fuzzed);

        return fuzzed;
    }

    /// <summary>
    /// Add random jitter to timestamp (-30s to +30s)
    /// Makes timing correlation attacks harder
    /// </summary>
    public DateTime AddJitter(DateTime timestamp)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);

        // Random number from -30 to +30 seconds
        var jitterSeconds = (BitConverter.ToInt32(bytes, 0) % 61) - 30;

        var jittered = timestamp.AddSeconds(jitterSeconds);

        _logger.LogTrace(
            "Added jitter to timestamp: {Original} -> {Jittered} ({Jitter}s)",
            timestamp,
            jittered,
            jitterSeconds);

        return jittered;
    }

    /// <summary>
    /// Apply fuzzing based on privacy level
    /// Higher privacy = more fuzzing
    /// </summary>
    public DateTime ApplyFuzzing(DateTime timestamp, FuzzingLevel level)
    {
        return level switch
        {
            FuzzingLevel.None => timestamp,
            FuzzingLevel.Low => FuzzToMinute(timestamp),
            FuzzingLevel.Medium => FuzzTo5Minutes(timestamp),
            FuzzingLevel.High => FuzzTo15Minutes(timestamp),
            FuzzingLevel.Maximum => AddJitter(FuzzTo15Minutes(timestamp)),
            _ => timestamp
        };
    }

    /// <summary>
    /// Check if two timestamps are within the same fuzzing window
    /// Useful for deduplication
    /// </summary>
    public bool AreSimilar(DateTime timestamp1, DateTime timestamp2, FuzzingLevel level)
    {
        var fuzzed1 = ApplyFuzzing(timestamp1, level);
        var fuzzed2 = ApplyFuzzing(timestamp2, level);

        return fuzzed1 == fuzzed2;
    }
}

/// <summary>
/// Fuzzing level for timestamps
/// </summary>
public enum FuzzingLevel
{
    /// <summary>
    /// No fuzzing - exact timestamps
    /// </summary>
    None = 0,

    /// <summary>
    /// Fuzz to nearest minute
    /// </summary>
    Low = 1,

    /// <summary>
    /// Fuzz to nearest 5 minutes
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Fuzz to nearest 15 minutes
    /// </summary>
    High = 3,

    /// <summary>
    /// Fuzz to 15 minutes + random jitter
    /// </summary>
    Maximum = 4
}
