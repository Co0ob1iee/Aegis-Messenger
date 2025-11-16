using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aegis.Core.Cryptography;

namespace Aegis.Core.Security;

/// <summary>
/// Manages duress PIN functionality
/// A duress PIN is a secondary PIN that when entered, triggers secure data wipe
/// </summary>
public class DuressPinManager
{
    private const int PinHashIterations = 100000;

    /// <summary>
    /// Set duress PIN
    /// </summary>
    /// <param name="duressPin">Duress PIN</param>
    /// <returns>Hashed duress PIN and salt</returns>
    public Task<(string hash, string salt)> SetDuressPinAsync(string duressPin)
    {
        if (string.IsNullOrWhiteSpace(duressPin))
            throw new ArgumentException("Duress PIN cannot be empty", nameof(duressPin));

        var salt = KeyDerivation.GenerateSalt();
        var hash = HashPin(duressPin, salt);

        return Task.FromResult((
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt)
        ));
    }

    /// <summary>
    /// Verify if entered PIN is the duress PIN
    /// </summary>
    /// <param name="enteredPin">PIN entered by user</param>
    /// <param name="storedHash">Stored duress PIN hash</param>
    /// <param name="storedSalt">Stored salt</param>
    /// <returns>True if duress PIN was entered</returns>
    public Task<bool> IsDuressPinAsync(string enteredPin, string storedHash, string storedSalt)
    {
        if (string.IsNullOrWhiteSpace(enteredPin))
            return Task.FromResult(false);

        try
        {
            var salt = Convert.FromBase64String(storedSalt);
            var hash = HashPin(enteredPin, salt);
            var expectedHash = Convert.FromBase64String(storedHash);

            bool isMatch = CryptographicOperations.FixedTimeEquals(hash, expectedHash);
            return Task.FromResult(isMatch);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Trigger duress mode - wipe sensitive data
    /// </summary>
    /// <param name="dataWipeCallback">Callback to execute data wipe</param>
    public async Task TriggerDuressModeAsync(Func<Task> dataWipeCallback)
    {
        // Execute data wipe
        if (dataWipeCallback != null)
        {
            await dataWipeCallback();
        }

        // Additional security measures
        // Clear sensitive memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Hash PIN using PBKDF2
    /// </summary>
    private byte[] HashPin(string pin, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(pin),
            salt,
            PinHashIterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32);
    }
}

/// <summary>
/// Duress mode configuration
/// </summary>
public class DuressModeConfig
{
    /// <summary>
    /// Whether duress PIN is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Hashed duress PIN
    /// </summary>
    public string? DuressPinHash { get; set; }

    /// <summary>
    /// Salt for duress PIN
    /// </summary>
    public string? DuressPinSalt { get; set; }

    /// <summary>
    /// What to wipe when duress PIN is entered
    /// </summary>
    public DuressWipeMode WipeMode { get; set; } = DuressWipeMode.AllData;
}

/// <summary>
/// Duress wipe mode
/// </summary>
public enum DuressWipeMode
{
    /// <summary>
    /// Wipe all messages and data
    /// </summary>
    AllData = 1,

    /// <summary>
    /// Wipe messages only
    /// </summary>
    MessagesOnly = 2,

    /// <summary>
    /// Show decoy data
    /// </summary>
    ShowDecoy = 3
}
