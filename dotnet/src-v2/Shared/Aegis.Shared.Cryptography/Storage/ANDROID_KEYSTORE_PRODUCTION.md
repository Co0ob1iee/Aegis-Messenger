# Android KeyStore - Production Implementation Guide

## Overview

The current `AndroidKeyStore.cs` implementation is a **simplified cross-platform compatible version**. For production Android applications, you should use the **AndroidX Security library** with hardware-backed key storage.

## Production Implementation

### Prerequisites

Add NuGet package to your Android project:
```xml
<PackageReference Include="Xamarin.AndroidX.Security.Crypto" Version="1.1.0" />
```

### Recommended Approach: EncryptedFile

Use AndroidX Security Crypto library's `EncryptedFile` for automatic key management:

```csharp
using AndroidX.Security.Crypto;
using Java.Security;

[SupportedOSPlatform("android21.0")]
public class ProductionAndroidKeyStore : IKeyStore
{
    private readonly Context _context;
    private readonly MasterKey _masterKey;

    public ProductionAndroidKeyStore(Context context)
    {
        _context = context;

        // Create or retrieve master key from Android KeyStore
        _masterKey = new MasterKey.Builder(_context)
            .SetKeyScheme(MasterKey.KeyScheme.Aes256Gcm)
            .SetUserAuthenticationRequired(true, 30)  // Require biometric auth, valid for 30 seconds
            .Build();
    }

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        var fileName = GetFileName(keyId, userId);
        var file = new Java.IO.File(_context.FilesDir, fileName);

        var encryptedFile = new EncryptedFile.Builder(
            _context,
            file,
            _masterKey,
            EncryptedFile.FileEncryptionScheme.Aes256GcmHkdfTStream
        ).Build();

        using var outputStream = encryptedFile.OpenFileOutput();
        await outputStream.WriteAsync(key);
    }

    public async Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
    {
        var fileName = GetFileName(keyId, userId);
        var file = new Java.IO.File(_context.FilesDir, fileName);

        if (!file.Exists())
            return null;

        var encryptedFile = new EncryptedFile.Builder(
            _context,
            file,
            _masterKey,
            EncryptedFile.FileEncryptionScheme.Aes256GcmHkdfTStream
        ).Build();

        using var inputStream = encryptedFile.OpenFileInput();
        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private string GetFileName(string keyId, Guid userId)
    {
        return $"{userId:N}_{keyId}.enc";
    }
}
```

### Alternative: Direct KeyStore API

For more control, use Android KeyStore API directly with `javax.crypto.Cipher`:

```csharp
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

public class DirectAndroidKeyStore : IKeyStore
{
    private const string ANDROID_KEYSTORE = "AndroidKeyStore";
    private const string KEY_ALGORITHM = "AES";
    private const string CIPHER_TRANSFORMATION = "AES/GCM/NoPadding";

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        // 1. Generate master key in Android KeyStore (if not exists)
        var masterKeyAlias = GetMasterKeyAlias(userId);
        EnsureMasterKeyExists(masterKeyAlias);

        // 2. Encrypt data key with master key
        var encryptedKey = EncryptWithMasterKey(key, masterKeyAlias);

        // 3. Store encrypted key to file
        var keyPath = GetKeyPath(keyId, userId);
        await File.WriteAllBytesAsync(keyPath, encryptedKey);
    }

    private void EnsureMasterKeyExists(string alias)
    {
        var keyStore = KeyStore.GetInstance(ANDROID_KEYSTORE);
        keyStore.Load(null);

        if (!keyStore.ContainsAlias(alias))
        {
            var keyGenerator = KeyGenerator.GetInstance(
                KeyProperties.KeyAlgorithmAes,
                ANDROID_KEYSTORE);

            var builder = new KeyGenParameterSpec.Builder(
                alias,
                KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetBlockModes(KeyProperties.BlockModeGcm)
                .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
                .SetKeySize(256)
                .SetUserAuthenticationRequired(true)
                .SetUserAuthenticationParameters(
                    30,  // Valid for 30 seconds
                    KeyProperties.AuthBiometricStrongOrDeviceCredential);

            keyGenerator.Init(builder.Build());
            keyGenerator.GenerateKey();
        }
    }

    private byte[] EncryptWithMasterKey(byte[] data, string keyAlias)
    {
        var keyStore = KeyStore.GetInstance(ANDROID_KEYSTORE);
        keyStore.Load(null);

        var secretKey = keyStore.GetKey(keyAlias, null) as ISecretKey;

        var cipher = Cipher.GetInstance(CIPHER_TRANSFORMATION);
        cipher.Init(CipherMode.EncryptMode, secretKey);

        var encryptedData = cipher.DoFinal(data);
        var iv = cipher.GetIV();

        // Prepend IV to encrypted data
        var result = new byte[iv.Length + encryptedData.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);

        return result;
    }
}
```

## Security Features

### Hardware-Backed Keys
- **StrongBox**: On supported devices (Android 9+), keys can be stored in a dedicated security chip
- **TEE (Trusted Execution Environment)**: Keys are protected by ARM TrustZone

Check if hardware-backed:
```csharp
var keyStore = KeyStore.GetInstance("AndroidKeyStore");
keyStore.Load(null);
var key = keyStore.GetKey(alias, null);

if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
{
    var factory = SecretKeyFactory.GetInstance(key.Algorithm, "AndroidKeyStore");
    var keyInfo = (KeyInfo)factory.GetKeySpec(key, Java.Lang.Class.FromType(typeof(KeyInfo)));

    bool isHardwareBacked = keyInfo.IsInsideSecureHardware;
    bool isStrongBox = Build.VERSION.SdkInt >= BuildVersionCodes.P &&
                       keyInfo.SecurityLevel == SecurityLevel.StrongBox;
}
```

### Biometric Authentication
Require biometric authentication (fingerprint, face unlock) to access keys:

```csharp
var builder = new KeyGenParameterSpec.Builder(alias, purpose)
    .SetUserAuthenticationRequired(true)
    .SetUserAuthenticationParameters(
        30,  // Valid for 30 seconds after biometric auth
        KeyProperties.AuthBiometricStrongOrDeviceCredential)
    .SetInvalidatedByBiometricEnrollment(true);  // Invalidate if new biometric enrolled
```

Prompt for biometric auth:
```csharp
using AndroidX.Biometric;

var biometricPrompt = new BiometricPrompt(activity, executor, callback);

var promptInfo = new BiometricPrompt.PromptInfo.Builder()
    .SetTitle("Unlock Aegis Messenger")
    .SetSubtitle("Authenticate to access your messages")
    .SetNegativeButtonText("Cancel")
    .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricStrong)
    .Build();

biometricPrompt.Authenticate(promptInfo, cryptoObject);
```

### Key Attestation
Verify key integrity and hardware backing:

```csharp
var attestation = keyStore.GetCertificateChain(alias);
// Verify attestation certificate against Google's root CA
```

## Migration from Simplified Implementation

1. **Gradual Migration**:
   ```csharp
   public class HybridKeyStore : IKeyStore
   {
       private readonly ProductionAndroidKeyStore _production;
       private readonly AndroidKeyStore _legacy;

       public async Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
       {
           // Try production first
           var key = await _production.RetrieveKeyAsync(keyId, userId);
           if (key != null) return key;

           // Fallback to legacy
           key = await _legacy.RetrieveKeyAsync(keyId, userId);
           if (key != null)
           {
               // Migrate to production
               await _production.StoreKeyAsync(keyId, key, userId);
               await _legacy.DeleteKeyAsync(keyId, userId);
           }

           return key;
       }
   }
   ```

2. **Testing**: Test on devices with/without hardware-backed storage
3. **Monitoring**: Log which key storage method is being used

## Best Practices

1. **Always use hardware-backed keys** when available
2. **Enable biometric authentication** for sensitive keys
3. **Invalidate keys** when biometrics change (prevent attacker from enrolling new fingerprint)
4. **Test on multiple devices** - hardware support varies
5. **Handle key loss** - implement key recovery mechanism
6. **Use StrongBox** when available (Pixel 3+, Samsung S9+, etc.)
7. **Audit key usage** - log all key access attempts

## Testing

Test matrix:
- ✅ Device with hardware-backed KeyStore (e.g., Pixel, Samsung flagship)
- ✅ Device without hardware-backed KeyStore (emulator, budget phone)
- ✅ Device with StrongBox (Android 9+ flagship)
- ✅ Biometric enrolled / not enrolled
- ✅ App reinstall (keys should be lost)
- ✅ OS upgrade (keys should persist)

## References

- [Android KeyStore System](https://developer.android.com/training/articles/keystore)
- [AndroidX Security Crypto](https://developer.android.com/topic/security/data)
- [Biometric Authentication](https://developer.android.com/training/sign-in/biometric-auth)
- [Key Attestation](https://developer.android.com/training/articles/security-key-attestation)
