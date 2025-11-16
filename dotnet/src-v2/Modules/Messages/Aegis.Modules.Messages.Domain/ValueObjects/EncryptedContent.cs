using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Messages.Domain.ValueObjects;

/// <summary>
/// Value object representing encrypted message content
/// Contains ciphertext encrypted with Signal Protocol
/// </summary>
public sealed class EncryptedContent : ValueObject
{
    public byte[] Ciphertext { get; }
    public bool IsPreKeyMessage { get; }

    private EncryptedContent(byte[] ciphertext, bool isPreKeyMessage)
    {
        Ciphertext = ciphertext;
        IsPreKeyMessage = isPreKeyMessage;
    }

    /// <summary>
    /// Create encrypted content
    /// </summary>
    public static Result<EncryptedContent> Create(byte[] ciphertext, bool isPreKeyMessage = false)
    {
        if (ciphertext == null || ciphertext.Length == 0)
        {
            return Result.Failure<EncryptedContent>(new Error(
                "EncryptedContent.Empty",
                "Encrypted content cannot be empty"));
        }

        return Result.Success(new EncryptedContent(ciphertext, isPreKeyMessage));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Ciphertext;
        yield return IsPreKeyMessage;
    }
}
