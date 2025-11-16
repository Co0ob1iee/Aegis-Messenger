# Linux KeyRing - Production Implementation Guide

## Overview

The current `LinuxKeyRingStore.cs` implementation is a **simplified file-based fallback**. For production Linux applications, you should use the **Secret Service API** (freedesktop.org standard) which is supported by:

- **GNOME Keyring** (most common on GNOME, Ubuntu, Fedora)
- **KDE KWallet** (KDE Plasma desktop)
- **Pass** (password-store.org)
- **Other implementations** following the Secret Service specification

## Production Implementation Approaches

### Option 1: libsecret (Recommended for GNOME)

libsecret is the standard C library for accessing the Secret Service API.

#### Prerequisites

Install development libraries:
```bash
# Ubuntu/Debian
sudo apt-get install libsecret-1-dev

# Fedora/RHEL
sudo dnf install libsecret-devel

# Arch Linux
sudo pacman -S libsecret
```

#### P/Invoke Implementation

```csharp
using System.Runtime.InteropServices;

[SupportedOSPlatform("linux")]
public class LibSecretKeyStore : IKeyStore
{
    private const string LibSecretName = "libsecret-1.so.0";

    // P/Invoke declarations
    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr secret_password_store_sync(
        IntPtr schema,
        string collection,
        string label,
        string password,
        IntPtr cancellable,
        out IntPtr error,
        string attribute_name1, string attribute_value1,
        IntPtr end);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    private static extern string secret_password_lookup_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attribute_name1, string attribute_value1,
        IntPtr end);

    [DllImport(LibSecretName, CallingConvention = CallingConvention.Cdecl)]
    private static extern bool secret_password_clear_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attribute_name1, string attribute_value1,
        IntPtr end);

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        var schema = GetSchema();
        var label = $"Aegis Messenger - {keyId}";
        var collection = "default"; // or "session" for temporary keys

        // Convert binary key to base64 for storage
        var keyBase64 = Convert.ToBase64String(key);

        var result = secret_password_store_sync(
            schema,
            collection,
            label,
            keyBase64,
            IntPtr.Zero,
            out IntPtr error,
            "application", "aegis-messenger",
            "user_id", userId.ToString(),
            "key_id", keyId,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            var errorMsg = Marshal.PtrToStringAnsi(error);
            throw new Exception($"Failed to store key: {errorMsg}");
        }
    }

    public async Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
    {
        var schema = GetSchema();

        var keyBase64 = secret_password_lookup_sync(
            schema,
            IntPtr.Zero,
            out IntPtr error,
            "application", "aegis-messenger",
            "user_id", userId.ToString(),
            "key_id", keyId,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            var errorMsg = Marshal.PtrToStringAnsi(error);
            throw new Exception($"Failed to retrieve key: {errorMsg}");
        }

        if (string.IsNullOrEmpty(keyBase64))
            return null;

        return Convert.FromBase64String(keyBase64);
    }

    public async Task<bool> DeleteKeyAsync(string keyId, Guid userId)
    {
        var schema = GetSchema();

        var result = secret_password_clear_sync(
            schema,
            IntPtr.Zero,
            out IntPtr error,
            "application", "aegis-messenger",
            "user_id", userId.ToString(),
            "key_id", keyId,
            IntPtr.Zero);

        if (error != IntPtr.Zero)
        {
            var errorMsg = Marshal.PtrToStringAnsi(error);
            throw new Exception($"Failed to delete key: {errorMsg}");
        }

        return result;
    }

    private IntPtr GetSchema()
    {
        // Define schema for our secrets
        // This would need proper SecretSchema structure marshaling
        // See libsecret documentation for details
        throw new NotImplementedException("Schema creation needs proper marshaling");
    }
}
```

### Option 2: D-Bus Secret Service API (Cross-Desktop)

Use D-Bus to communicate with any Secret Service provider.

#### Prerequisites

Install D-Bus .NET library:
```bash
dotnet add package Tmds.DBus
```

#### D-Bus Implementation

```csharp
using Tmds.DBus;

[DBusInterface("org.freedesktop.Secret.Collection")]
interface ISecretCollection : IDBusObject
{
    Task<ObjectPath> CreateItemAsync(
        IDictionary<string, object> properties,
        (ObjectPath, byte[]) secret,
        bool replace);

    Task<ObjectPath[]> SearchItemsAsync(IDictionary<string, string> attributes);
}

[DBusInterface("org.freedesktop.Secret.Item")]
interface ISecretItem : IDBusObject
{
    Task<(ObjectPath session, byte[] value)> GetSecretAsync(ObjectPath session);
    Task DeleteAsync();
}

[DBusInterface("org.freedesktop.Secret.Service")]
interface ISecretService : IDBusObject
{
    Task<(ObjectPath output, ObjectPath result)> OpenSessionAsync(
        string algorithm,
        object input);
}

[SupportedOSPlatform("linux")]
public class DBusSecretServiceKeyStore : IKeyStore
{
    private Connection _connection;
    private ISecretService _service;
    private ObjectPath _session;

    public async Task InitializeAsync()
    {
        _connection = new Connection(Address.Session);
        await _connection.ConnectAsync();

        _service = _connection.CreateProxy<ISecretService>(
            "org.freedesktop.secrets",
            "/org/freedesktop/secrets");

        // Open session with encryption
        var (_, session) = await _service.OpenSessionAsync("plain", new Variant(string.Empty));
        _session = session;
    }

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        var collection = _connection.CreateProxy<ISecretCollection>(
            "org.freedesktop.secrets",
            "/org/freedesktop/secrets/collection/default");

        var properties = new Dictionary<string, object>
        {
            ["org.freedesktop.Secret.Item.Label"] = $"Aegis Messenger - {keyId}",
            ["org.freedesktop.Secret.Item.Attributes"] = new Dictionary<string, string>
            {
                ["application"] = "aegis-messenger",
                ["user_id"] = userId.ToString(),
                ["key_id"] = keyId
            }
        };

        var secret = (_session, key);
        await collection.CreateItemAsync(properties, secret, true);
    }

    public async Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
    {
        var collection = _connection.CreateProxy<ISecretCollection>(
            "org.freedesktop.secrets",
            "/org/freedesktop/secrets/collection/default");

        var attributes = new Dictionary<string, string>
        {
            ["application"] = "aegis-messenger",
            ["user_id"] = userId.ToString(),
            ["key_id"] = keyId
        };

        var items = await collection.SearchItemsAsync(attributes);
        if (items.Length == 0)
            return null;

        var item = _connection.CreateProxy<ISecretItem>(
            "org.freedesktop.secrets",
            items[0]);

        var (_, value) = await item.GetSecretAsync(_session);
        return value;
    }

    public async Task<bool> DeleteKeyAsync(string keyId, Guid userId)
    {
        var collection = _connection.CreateProxy<ISecretCollection>(
            "org.freedesktop.secrets",
            "/org/freedesktop/secrets/collection/default");

        var attributes = new Dictionary<string, string>
        {
            ["application"] = "aegis-messenger",
            ["user_id"] = userId.ToString(),
            ["key_id"] = keyId
        };

        var items = await collection.SearchItemsAsync(attributes);
        if (items.Length == 0)
            return false;

        foreach (var itemPath in items)
        {
            var item = _connection.CreateProxy<ISecretItem>(
                "org.freedesktop.secrets",
                itemPath);
            await item.DeleteAsync();
        }

        return true;
    }
}
```

### Option 3: KWallet (KDE Specific)

For KDE environments, use KWallet D-Bus API.

```csharp
[DBusInterface("org.kde.KWallet")]
interface IKWallet : IDBusObject
{
    Task<int> OpenAsync(string wallet, long wId, string appid);
    Task<int> WritePasswordAsync(int handle, string folder, string key, string value, string appid);
    Task<string> ReadPasswordAsync(int handle, string folder, string key, string appid);
    Task<int> RemoveEntryAsync(int handle, string folder, string key, string appid);
}

public class KWalletKeyStore : IKeyStore
{
    private Connection _connection;
    private IKWallet _wallet;
    private int _handle;

    public async Task InitializeAsync()
    {
        _connection = new Connection(Address.Session);
        await _connection.ConnectAsync();

        _wallet = _connection.CreateProxy<IKWallet>(
            "org.kde.kwalletd5",
            "/modules/kwalletd5");

        _handle = await _wallet.OpenAsync("kdewallet", 0, "aegis-messenger");
    }

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        var keyBase64 = Convert.ToBase64String(key);
        var folder = $"aegis-messenger-{userId:N}";
        await _wallet.WritePasswordAsync(_handle, folder, keyId, keyBase64, "aegis-messenger");
    }
}
```

## Hardware Security Module (TPM 2.0)

For enhanced security on systems with TPM 2.0:

```bash
# Install TPM tools
sudo apt-get install tpm2-tools libtpm2-tss-dev

# Create persistent key in TPM
tpm2_createprimary -C e -g sha256 -G rsa -c primary.ctx
tpm2_create -C primary.ctx -g sha256 -G rsa -r key.priv -u key.pub
tpm2_load -C primary.ctx -r key.priv -u key.pub -c key.ctx
tpm2_evictcontrol -C o -c key.ctx 0x81010001
```

Then use TPM for key encryption:
```csharp
// Use tpm2-tools via Process or P/Invoke to libtss2
// Keys are encrypted/decrypted by TPM hardware
```

## Collections and Access Control

### Session vs Login Collection

```csharp
// Session collection - cleared on logout
var sessionCollection = "/org/freedesktop/secrets/collection/session";

// Login collection - persists until explicitly deleted
var loginCollection = "/org/freedesktop/secrets/collection/login";

// Default collection - usually points to "login"
var defaultCollection = "/org/freedesktop/secrets/aliases/default";
```

### Lock/Unlock

```csharp
[DBusInterface("org.freedesktop.Secret.Collection")]
interface ISecretCollection : IDBusObject
{
    Task LockAsync();
    Task UnlockAsync();

    Task<bool> GetLockedAsync();
}

// Lock collection when app goes to background
await collection.LockAsync();

// Unlock will prompt user for master password
await collection.UnlockAsync();
```

## Desktop Environment Detection

```csharp
public static string DetectDesktopEnvironment()
{
    var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
    var session = Environment.GetEnvironmentVariable("DESKTOP_SESSION");

    if (desktop?.Contains("GNOME") == true || session?.Contains("gnome") == true)
        return "GNOME";

    if (desktop?.Contains("KDE") == true || session?.Contains("plasma") == true)
        return "KDE";

    if (desktop?.Contains("XFCE") == true)
        return "XFCE";

    // Check if Secret Service is available
    try
    {
        var connection = new Connection(Address.Session);
        connection.ConnectAsync().Wait();
        var service = connection.CreateProxy<ISecretService>(
            "org.freedesktop.secrets",
            "/org/freedesktop/secrets");
        return "SecretService";
    }
    catch
    {
        return "Unknown";
    }
}
```

## Fallback Strategy

```csharp
public class LinuxKeyStoreFactory
{
    public static IKeyStore Create(ILogger logger)
    {
        var desktop = DetectDesktopEnvironment();

        try
        {
            switch (desktop)
            {
                case "GNOME":
                    return new LibSecretKeyStore(logger);

                case "KDE":
                    // Try KWallet first, fallback to Secret Service
                    try
                    {
                        var kwallet = new KWalletKeyStore(logger);
                        kwallet.InitializeAsync().Wait();
                        return kwallet;
                    }
                    catch
                    {
                        goto case "SecretService";
                    }

                case "SecretService":
                    var secretService = new DBusSecretServiceKeyStore(logger);
                    secretService.InitializeAsync().Wait();
                    return secretService;

                default:
                    logger.LogWarning(
                        "No KeyRing available, falling back to file-based storage");
                    return new LinuxKeyRingStore(logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize KeyRing, using file-based fallback");
            return new LinuxKeyRingStore(logger);
        }
    }
}
```

## Security Considerations

### File Permissions

If using file-based fallback:
```bash
# Keys directory should be 700 (rwx------)
chmod 700 ~/.local/share/AegisMessenger/keys

# Key files should be 600 (rw-------)
chmod 600 ~/.local/share/AegisMessenger/keys/*/*.key
```

### SELinux/AppArmor

Add policies for accessing Secret Service:
```
# AppArmor profile
/usr/bin/aegis-messenger {
  # Allow D-Bus communication with Secret Service
  dbus send
    bus=session
    interface=org.freedesktop.Secret.*
    peer=(name=org.freedesktop.secrets),
}
```

## Testing

```bash
# Test GNOME Keyring
secret-tool store --label='Test' application aegis-messenger key_id test123
secret-tool lookup application aegis-messenger key_id test123
secret-tool clear application aegis-messenger key_id test123

# Test KWallet
kwalletcli -f aegis-messenger -e test123
kwalletcli -f aegis-messenger -p test123
```

## Best Practices

1. **Always prefer Secret Service API** over file-based storage
2. **Use session collection** for temporary keys
3. **Use login collection** for persistent keys
4. **Implement proper fallback** for headless systems
5. **Test on multiple desktops** (GNOME, KDE, XFCE)
6. **Handle locked collections** gracefully
7. **Set proper D-Bus timeouts** for user interactions

## References

- [Secret Service API Specification](https://specifications.freedesktop.org/secret-service/)
- [libsecret Documentation](https://developer.gnome.org/libsecret/)
- [KWallet D-Bus API](https://api.kde.org/frameworks/kwallet/html/)
- [Tmds.DBus Library](https://github.com/tmds/Tmds.DBus)
