using Aegis.Shared.Cryptography.Services;

namespace Aegis.Shared.Cryptography.Interfaces;

/// <summary>
/// Service for fuzzing timestamps to prevent timing correlation attacks
/// </summary>
public interface ITimestampFuzzingService
{
    /// <summary>
    /// Fuzz timestamp to nearest minute
    /// </summary>
    DateTime FuzzToMinute(DateTime timestamp);

    /// <summary>
    /// Fuzz timestamp to nearest 5 minutes
    /// </summary>
    DateTime FuzzTo5Minutes(DateTime timestamp);

    /// <summary>
    /// Fuzz timestamp to nearest 15 minutes
    /// </summary>
    DateTime FuzzTo15Minutes(DateTime timestamp);

    /// <summary>
    /// Add random jitter to timestamp (-30s to +30s)
    /// </summary>
    DateTime AddJitter(DateTime timestamp);

    /// <summary>
    /// Apply fuzzing based on privacy level
    /// </summary>
    DateTime ApplyFuzzing(DateTime timestamp, FuzzingLevel level);

    /// <summary>
    /// Check if two timestamps are within the same fuzzing window
    /// </summary>
    bool AreSimilar(DateTime timestamp1, DateTime timestamp2, FuzzingLevel level);
}
