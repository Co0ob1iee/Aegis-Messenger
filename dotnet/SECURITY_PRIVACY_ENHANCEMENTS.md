# Aegis Messenger - Security & Privacy Enhancements

## Analiza Obecnego Stanu

### Mocne Strony
1. **End-to-End Encryption (E2EE)**
   - Signal Protocol z X3DH key agreement
   - Double Ratchet dla forward i post-compromise security
   - Safety numbers do weryfikacji kluczy

2. **Bezpieczeństwo Autoryzacji**
   - JWT z osobnymi access i refresh tokens
   - PBKDF2 z 310,000 iteracjami dla haseł
   - Automatyczne revokowanie starych refresh tokens (max 5)

3. **Podstawowa Kontrola Prywatności**
   - ShowOnlineStatus flag
   - Blokowanie użytkowników
   - Soft delete wiadomości

### Krytyczne Luki w Prywatności

#### 1. **Metadata Leakage** (CRITICAL)
**Problem:**
```csharp
public class Message {
    public Guid SenderId { get; private set; }      // Serwer zna nadawcę
    public Guid RecipientId { get; private set; }   // Serwer zna odbiorcę
    public DateTime SentAt { get; private set; }    // Precyzyjny timestamp
}
```

**Co ujawnia:**
- Kto do kogo pisze
- Kiedy dokładnie (co do milisekundy)
- Jak często
- Social graph użytkownika
- Wzorce komunikacji (timing analysis)

**Rozwiązanie:**
- Sealed Sender (ukrycie nadawcy przed serwerem)
- Timestamp fuzzing (zaokrąglanie do 1-5 minut)
- Message padding (ukrycie długości)
- Dummy traffic (ukrycie częstotliwości)

#### 2. **Brak Message Padding**
**Problem:**
Długość zaszyfrowanej wiadomości ujawnia informację o treści.

**Statystyki:**
- "tak" (3 bajty) vs "Zgadzam się z tobą w pełni" (28 bajtów)
- Atakujący może wydedukować typ wiadomości

**Rozwiązanie:**
```csharp
// Padding do najbliższej potęgi 2 lub stałych rozmiarów
byte[] PadMessage(byte[] plaintext)
{
    int[] sizes = { 256, 512, 1024, 2048, 4096, 8192, 16384, 32768 };
    int targetSize = sizes.First(s => s >= plaintext.Length);
    // Pad with random bytes + length marker
}
```

#### 3. **InMemory Key Storage**
**Problem:**
```csharp
private readonly SignalProtocolStore _protocolStore = new InMemorySignalProtocolStore();
```

**Zagrożenia:**
- Klucze tracone przy restarcie aplikacji
- Brak ochrony przed memory dumps
- Brak multi-device support
- Niemożliwy backup

**Rozwiązanie:**
- Windows: DPAPI (Data Protection API)
- Android: KeyStore z hardware-backed keys
- Encryption at rest z key derivation od hasła użytkownika

#### 4. **Precyzyjne Timestampy**
**Problem:**
```csharp
SentAt = DateTime.UtcNow;  // 2025-11-16T14:32:18.1234567Z
```

**Zagrożenia:**
- Timing correlation attacks
- Profilowanie wzorców aktywności
- Timezone leakage
- Identyfikacja użytkownika po wzorcach

**Rozwiązanie:**
```csharp
// Zaokrąglanie do najbliższej minuty
DateTime FuzzTimestamp(DateTime timestamp)
{
    return new DateTime(
        timestamp.Year, timestamp.Month, timestamp.Day,
        timestamp.Hour, timestamp.Minute, 0, DateTimeKind.Utc);
}
```

#### 5. **Brak Disappearing Messages**
**Problem:**
- Wiadomości przechowywane na zawsze
- Zwiększone ryzyko w razie kompromitacji
- Brak kontroli użytkownika nad danymi

**Rozwiązanie:**
```csharp
public class Message {
    public TimeSpan? ExpiresAfter { get; private set; }  // 1h, 24h, 7d
    public DateTime? ExpiresAt { get; private set; }

    public void SetExpiration(TimeSpan duration)
    {
        ExpiresAfter = duration;
        ExpiresAt = ReadAt ?? DeliveredAt ?? SentAt + duration;
    }
}
```

## Plan Implementacji

### FAZA 1: Privacy Settings & Disappearing Messages (PRIORYTET)

#### 1.1 Privacy Settings Module
```csharp
public class PrivacySettings : ValueObject
{
    // Kto może widzieć mój status online
    public PrivacyLevel OnlineStatusVisibility { get; private set; }

    // Kto może widzieć "last seen"
    public PrivacyLevel LastSeenVisibility { get; private set; }

    // Kto może widzieć zdjęcie profilowe
    public PrivacyLevel ProfilePictureVisibility { get; private set; }

    // Kto może widzieć bio
    public PrivacyLevel BioVisibility { get; private set; }

    // Potwierdzenia odczytu (read receipts)
    public bool SendReadReceipts { get; private set; }

    // Potwierdzenia dostarczenia
    public bool SendDeliveryReceipts { get; private set; }

    // Wskaźnik pisania
    public bool SendTypingIndicators { get; private set; }

    // Domyślny czas wygaśnięcia wiadomości
    public TimeSpan? DefaultMessageExpiration { get; private set; }
}

public enum PrivacyLevel
{
    Everyone,      // Wszyscy
    Contacts,      // Tylko kontakty
    Nobody         // Nikt
}
```

#### 1.2 Disappearing Messages
```csharp
// Dodać do Message entity
public TimeSpan? DisappearDuration { get; private set; }
public DateTime? DisappearsAt { get; private set; }

public Result SetDisappearing(TimeSpan duration)
{
    // Opcje: 30s, 1m, 5m, 30m, 1h, 8h, 24h, 7d
    DisappearDuration = duration;

    // Zaczyna się od przeczytania
    if (ReadAt.HasValue)
        DisappearsAt = ReadAt.Value + duration;

    return Result.Success();
}

// Background job - usuwa wygasłe wiadomości
public class MessageExpirationService
{
    public async Task DeleteExpiredMessagesAsync()
    {
        var expired = await _repository.GetExpiredMessagesAsync();

        foreach (var message in expired)
        {
            await SecureDeleteAsync(message);  // Overwrite przed usunięciem
        }
    }
}
```

### FAZA 2: Metadata Protection

#### 2.1 Message Padding
```csharp
public class MessagePaddingService : IMessagePaddingService
{
    private static readonly int[] PaddingSizes =
        { 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536 };

    public byte[] PadMessage(byte[] plaintext)
    {
        var targetSize = PaddingSizes.First(s => s >= plaintext.Length + 2);
        var padded = new byte[targetSize];

        // [2 bytes: original length][plaintext][random padding]
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)plaintext.Length), 0, padded, 0, 2);
        Buffer.BlockCopy(plaintext, 0, padded, 2, plaintext.Length);

        // Random padding
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(padded, 2 + plaintext.Length, targetSize - 2 - plaintext.Length);

        return padded;
    }

    public byte[] UnpadMessage(byte[] padded)
    {
        var originalLength = BitConverter.ToUInt16(padded, 0);
        var plaintext = new byte[originalLength];
        Buffer.BlockCopy(padded, 2, plaintext, 0, originalLength);
        return plaintext;
    }
}
```

#### 2.2 Timestamp Fuzzing
```csharp
public class TimestampFuzzingService
{
    // Zaokrąglanie do najbliższej minuty
    public DateTime FuzzToMinute(DateTime timestamp)
    {
        return new DateTime(
            timestamp.Year, timestamp.Month, timestamp.Day,
            timestamp.Hour, timestamp.Minute, 0, DateTimeKind.Utc);
    }

    // Zaokrąglanie do najbliższych 5 minut (więcej prywatności)
    public DateTime FuzzTo5Minutes(DateTime timestamp)
    {
        var minute = (timestamp.Minute / 5) * 5;
        return new DateTime(
            timestamp.Year, timestamp.Month, timestamp.Day,
            timestamp.Hour, minute, 0, DateTimeKind.Utc);
    }

    // Dodanie losowego szumu (-30s do +30s)
    public DateTime AddJitter(DateTime timestamp)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var jitter = BitConverter.ToInt32(bytes, 0) % 60 - 30;  // -30 to +30 seconds
        return timestamp.AddSeconds(jitter);
    }
}
```

#### 2.3 Sealed Sender (Zaawansowane)
```csharp
// Zaszyfrowana koperta z ukrytym nadawcą
public class SealedSenderEnvelope
{
    public byte[] EncryptedSender { get; private set; }      // Zaszyfrowany nadawca
    public byte[] EncryptedContent { get; private set; }     // Zaszyfrowana treść
    public byte[] ServerAuthTag { get; private set; }        // Tag dla serwera (bez info o nadawcy)

    // Tylko odbiorca może odszyfrować i poznać nadawcę
    // Serwer widzi tylko: "ktoś wysłał coś do userId X"
}

public class SealedSenderService
{
    public async Task<SealedSenderEnvelope> SealMessageAsync(
        Guid senderId,
        Guid recipientId,
        byte[] content)
    {
        // 1. Pobierz klucz publiczny odbiorcy (sender key)
        var recipientSenderKey = await _keyService.GetSenderKeyAsync(recipientId);

        // 2. Zaszyfruj info o nadawcy kluczem odbiorcy
        var senderInfo = new SenderInfo(senderId, DateTime.UtcNow);
        var encryptedSender = await EncryptAsync(senderInfo, recipientSenderKey);

        // 3. Zaszyfruj treść normalnie (Signal Protocol)
        var encryptedContent = await _signalProtocol.EncryptMessageAsync(recipientId, content);

        // 4. Utwórz tag dla serwera (HMAC bez informacji o nadawcy)
        var serverAuthTag = ComputeServerAuthTag(recipientId, encryptedContent);

        return new SealedSenderEnvelope
        {
            EncryptedSender = encryptedSender,
            EncryptedContent = encryptedContent,
            ServerAuthTag = serverAuthTag
        };
    }
}
```

### FAZA 3: Secure Key Storage

#### 3.1 Platform-Specific Key Storage
```csharp
public interface ISecureKeyStorage
{
    Task StoreKeyAsync(string keyId, byte[] key);
    Task<byte[]> RetrieveKeyAsync(string keyId);
    Task DeleteKeyAsync(string keyId);
    bool IsHardwareBacked { get; }
}

// Windows Implementation
public class WindowsKeyStorage : ISecureKeyStorage
{
    public async Task StoreKeyAsync(string keyId, byte[] key)
    {
        // DPAPI - Windows Data Protection API
        var encrypted = ProtectedData.Protect(
            key,
            entropy: GetDeviceEntropy(),
            scope: DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(GetKeyPath(keyId), encrypted);
    }

    public async Task<byte[]> RetrieveKeyAsync(string keyId)
    {
        var encrypted = await File.ReadAllBytesAsync(GetKeyPath(keyId));

        return ProtectedData.Unprotect(
            encrypted,
            entropy: GetDeviceEntropy(),
            scope: DataProtectionScope.CurrentUser);
    }
}

// Android Implementation (via P/Invoke or Xamarin.Essentials)
public class AndroidKeyStorage : ISecureKeyStorage
{
    // Android KeyStore - hardware-backed gdy dostępne
    public bool IsHardwareBacked =>
        AndroidKeyStore.IsHardwareBackedKeystoreAvailable();

    public async Task StoreKeyAsync(string keyId, byte[] key)
    {
        // Użyj Android KeyStore API
        // Generuje master key w hardware (jeśli dostępne)
        // Szyfruje klucz aplikacji tym master key
    }
}
```

#### 3.2 Key Rotation
```csharp
public class KeyRotationService
{
    // Automatyczna rotacja kluczy co 30 dni
    public async Task RotateIdentityKeysAsync(Guid userId)
    {
        // 1. Generuj nową parę kluczy
        var newKeyPair = await _signalProtocol.GenerateIdentityKeyPairAsync();

        // 2. Zachowaj stary klucz przez 90 dni (dla starych sesji)
        await ArchiveOldKeyAsync(userId, oldKeyPair, retentionDays: 90);

        // 3. Zaktualizuj klucz
        await UpdateIdentityKeyAsync(userId, newKeyPair);

        // 4. Generuj nowe pre-keys
        await _signalProtocol.GeneratePreKeyBundleAsync(userId, newKeyPair, registrationId);

        // 5. Notify kontakty o zmianie klucza
        await NotifyKeyChangeAsync(userId);
    }
}
```

### FAZA 4: Security Audit & Rate Limiting

#### 4.1 Security Audit Log
```csharp
public class SecurityAuditLog : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public SecurityEventType EventType { get; private set; }
    public string IpAddress { get; private set; }
    public string UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Details { get; private set; }
    public bool IsSuccessful { get; private set; }
}

public enum SecurityEventType
{
    Login,
    LoginFailed,
    PasswordChanged,
    KeyRotated,
    KeyVerified,
    SessionCreated,
    DeviceAdded,
    AccountRecovery,
    PrivacySettingsChanged,
    UserBlocked,
    MessageDeleted
}

// Automatyczne logowanie
public class SecurityAuditInterceptor : INotificationHandler<INotification>
{
    public async Task Handle(INotification notification, CancellationToken ct)
    {
        if (notification is ISecurityEvent securityEvent)
        {
            await _auditLog.LogAsync(securityEvent);
        }
    }
}
```

#### 4.2 Rate Limiting
```csharp
public class RateLimitingService
{
    // Sliding window rate limiter
    private readonly Dictionary<string, Queue<DateTime>> _requests = new();

    public bool AllowRequest(string userId, string operation)
    {
        var key = $"{userId}:{operation}";
        var window = GetWindow(operation);
        var limit = GetLimit(operation);

        if (!_requests.ContainsKey(key))
            _requests[key] = new Queue<DateTime>();

        var queue = _requests[key];
        var now = DateTime.UtcNow;

        // Usuń stare requesty poza oknem
        while (queue.Count > 0 && queue.Peek() < now - window)
            queue.Dequeue();

        if (queue.Count >= limit)
            return false;  // Rate limit exceeded

        queue.Enqueue(now);
        return true;
    }

    private TimeSpan GetWindow(string operation) => operation switch
    {
        "login" => TimeSpan.FromMinutes(15),
        "send_message" => TimeSpan.FromMinutes(1),
        "register" => TimeSpan.FromHours(1),
        _ => TimeSpan.FromMinutes(5)
    };

    private int GetLimit(string operation) => operation switch
    {
        "login" => 5,           // 5 prób logowania / 15 min
        "send_message" => 60,   // 60 wiadomości / minutę
        "register" => 3,        // 3 rejestracje / godzinę z IP
        _ => 30
    };
}
```

### FAZA 5: Anonymous Accounts

#### 5.1 Opcjonalna Anonimowość
```csharp
public class User : AggregateRoot<Guid>
{
    public Username? Username { get; private set; }  // Opcjonalne!
    public Email? Email { get; private set; }        // Opcjonalne!
    public PhoneNumber? PhoneNumber { get; private set; }

    // Dla anonimowych kont - generowany identyfikator
    public string? AnonymousId { get; private set; }  // "user_a3b8c9d2"

    public bool IsAnonymous =>
        Username == null && Email == null && PhoneNumber == null;

    public static Result<User> CreateAnonymous(HashedPassword password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            AnonymousId = GenerateAnonymousId(),
            Password = password,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(user);
    }

    private static string GenerateAnonymousId()
    {
        // Generuj krótki, przyjazny identyfikator
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[6];
        rng.GetBytes(bytes);
        return $"user_{Convert.ToHexString(bytes).ToLower()}";
    }
}
```

#### 5.2 Account Recovery dla Anonimowych Kont
```csharp
// Recovery codes - jedyna metoda odzyskania anonimowego konta
public class RecoveryCode : ValueObject
{
    public string Code { get; private set; }  // 24-znakowy kod
    public DateTime GeneratedAt { get; private set; }
    public bool IsUsed { get; private set; }

    public static List<RecoveryCode> GenerateCodes(int count = 10)
    {
        var codes = new List<RecoveryCode>();

        for (int i = 0; i < count; i++)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);

            // Format: XXXX-XXXX-XXXX-XXXX-XXXX-XXXX
            var code = Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 24);

            codes.Add(new RecoveryCode
            {
                Code = FormatCode(code),
                GeneratedAt = DateTime.UtcNow,
                IsUsed = false
            });
        }

        return codes;
    }
}
```

## Priorytetyzacja Implementacji

### MUST HAVE (Faza 1 - Tydzień 1)
1. ✅ Privacy Settings (OnlineStatus, LastSeen, ReadReceipts)
2. ✅ Disappearing Messages
3. ✅ Message Padding
4. ✅ Timestamp Fuzzing

### SHOULD HAVE (Faza 2 - Tydzień 2)
5. ✅ Secure Key Storage (Windows DPAPI)
6. ✅ Security Audit Log
7. ✅ Rate Limiting
8. ⚠️ Sealed Sender (częściowo - wymaga więcej R&D)

### NICE TO HAVE (Faza 3 - Przyszłość)
9. ⏳ Anonymous Accounts
10. ⏳ Multi-device Support
11. ⏳ Backup Keys
12. ⏳ Advanced Metadata Protection

## Podsumowanie

Implementacja tych ulepszeń podniesie Aegis Messenger do poziomu aplikacji takich jak Signal czy Session:

- **Privacy by Design** - minimalizacja danych od początku
- **Metadata Protection** - serwer wie jak najmniej
- **User Control** - użytkownik kontroluje swoją prywatność
- **Security in Depth** - wiele warstw ochrony
- **Anonymous Option** - możliwość pełnej anonimowości

Kolejny krok: Implementacja Fazy 1 (Privacy Settings + Disappearing Messages).
