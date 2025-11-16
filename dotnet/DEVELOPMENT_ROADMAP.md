# 🛡️ Plan Rozwoju Aegis Messenger - Bezpieczeństwo, Higiena i Nowe Funkcjonalności

## 📊 ANALIZA OBECNEGO STANU

### ⚠️ KRYTYCZNE Luki Bezpieczeństwa

| Priorytet | Problem | Lokalizacja | Ryzyko |
|-----------|---------|-------------|--------|
| 🔴 **CRITICAL** | In-memory storage kluczy Signal Protocol | `SignalSessionManager.cs:317` | **WYSOKIE** - Klucze tracone po restarcie |
| 🔴 **CRITICAL** | Domyślny JWT key w kodzie | `Program.cs:76` | **KRYTYCZNE** - Łatwy do zgadnięcia |
| 🔴 **CRITICAL** | Brak szyfrowania sesji w bazie | `AegisDbContext.cs` | **WYSOKIE** - Sesje niezaszyfrowane |
| 🟠 **HIGH** | CORS AllowAll policy | `Program.cs` | **ŚREDNIE** - Podatność na CSRF |
| 🟠 **HIGH** | Brak rate limiting | Cały backend | **WYSOKIE** - Możliwy DoS |
| 🟠 **HIGH** | Brak walidacji rozmiaru plików | Nie zaimplementowane | **ŚREDNIE** - Możliwe wyczerpanie dysku |
| 🟡 **MEDIUM** | Brak session expiration | `SignalSessionManager.cs` | **ŚREDNIE** - Sesje żyją wiecznie |
| 🟡 **MEDIUM** | Szczegółowe error messages | Różne kontrolery | **NISKIE** - Information disclosure |

### ⚙️ Problemy Higieny Kodu

| Kategoria | Problem | Wpływ |
|-----------|---------|-------|
| **Dependency Injection** | Brak rejestracji wszystkich serwisów | Trudność w testowaniu |
| **Logging** | Niespójne poziomy logowania | Trudność w debugowaniu |
| **Testing** | Brak testów jednostkowych | Ryzyko regresji |
| **Documentation** | Brak XML docs w niektórych klasach | Trudność w maintenance |
| **Error Handling** | Globalna obsługa błędów nie zaimplementowana | Niekonsystentne odpowiedzi |
| **Configuration** | Secrets w appsettings.json | Łatwy wyciek danych |

### 📈 Brakujące Funkcjonalności

- ❌ Disappearing messages (wiadomości znikające)
- ❌ Voice/Video calls (połączenia głosowe/wideo)
- ❌ Push notifications (Android)
- ❌ Message reactions (reakcje emoji)
- ❌ Message search (wyszukiwanie)
- ❌ Backup/Restore (kopia zapasowa)
- ❌ Multi-device sync (synchronizacja urządzeń)
- ❌ Contact discovery (odkrywanie kontaktów)

---

## 🎯 ROADMAP ROZWOJU

### FAZA 1: 🔥 KRYTYCZNE POPRAWKI BEZPIECZEŃSTWA (1-2 tygodnie)

#### 1.1 Persistent Signal Protocol Store

**Zadanie:** Zamiana `InMemorySignalProtocolStore` na implementację bazodanową.

**Implementacja:**
```csharp
// Aegis.Data/Entities/SignalProtocolEntities.cs
public class StoredSessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string RemoteAddress { get; set; }
    public byte[] SessionData { get; set; } // Encrypted
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
}

public class StoredPreKeyEntity
{
    public Guid UserId { get; set; }
    public uint PreKeyId { get; set; }
    public byte[] KeyData { get; set; } // Encrypted
    public DateTime CreatedAt { get; set; }
}
```

**Pliki do stworzenia:**
- `Aegis.Data/Entities/SignalProtocolEntities.cs`
- `Aegis.Core/Cryptography/SignalProtocol/DatabaseSignalProtocolStore.cs`
- Migration dla nowych tabel

#### 1.2 Secure JWT Configuration

**Zadanie:** Usunięcie hardcoded JWT key i wymuszenie konfiguracji.

**Implementacja:**
```csharp
// Program.cs
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT Key must be configured in appsettings.json or user secrets");

if (jwtKey.Length < 64)
    throw new InvalidOperationException(
        "JWT Key must be at least 64 characters long");
```

**Dodatkowo:**
- User Secrets dla development
- Azure Key Vault / AWS Secrets Manager dla production
- Rotacja kluczy co 90 dni

#### 1.3 Encrypted Session Storage

**Zadanie:** Szyfrowanie sesji Signal Protocol w bazie danych.

**Implementacja:**
```csharp
// Aegis.Data/Services/SessionEncryptionService.cs
public class SessionEncryptionService
{
    private readonly byte[] _masterKey;

    public byte[] EncryptSessionData(SessionRecord session)
    {
        var serialized = session.serialize();
        return ProtectedData.Protect(serialized, _masterKey,
            DataProtectionScope.CurrentUser);
    }

    public SessionRecord DecryptSessionData(byte[] encrypted)
    {
        var decrypted = ProtectedData.Unprotect(encrypted, _masterKey,
            DataProtectionScope.CurrentUser);
        return new SessionRecord(decrypted);
    }
}
```

#### 1.4 CORS Policy Hardening

**Zadanie:** Ograniczenie CORS do znanych origin.

**Implementacja:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AegisPolicy", policy =>
    {
        policy.WithOrigins(
                "https://aegis-desktop.local",
                "https://localhost:7001"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

#### 1.5 Rate Limiting

**Zadanie:** Implementacja rate limiting dla API endpoints.

**Implementacja:**
```csharp
// NuGet: AspNetCoreRateLimit
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = "1m",
            Limit = 5 // 5 prób na minutę
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/messages",
            Period = "1s",
            Limit = 10 // 10 wiadomości na sekundę
        }
    };
});
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();
```

**Pliki do stworzenia:**
- `Aegis.Backend/Middleware/RateLimitingMiddleware.cs`

---

### FAZA 2: 🧹 HIGIENA KODU I INFRASTRUKTURA (2-3 tygodnie)

#### 2.1 Global Error Handling

**Zadanie:** Middleware do spójnej obsługi błędów.

**Implementacja:**
```csharp
// Aegis.Backend/Middleware/GlobalExceptionMiddleware.cs
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(context, ex, 400);
        }
        catch (UnauthorizedException ex)
        {
            await HandleExceptionAsync(context, ex, 401);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex, 500);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context, Exception ex, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = ex.Message,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
```

**Pliki do stworzenia:**
- `Aegis.Backend/Middleware/GlobalExceptionMiddleware.cs`
- `Aegis.Backend/Models/ErrorResponse.cs`
- `Aegis.Core/Exceptions/` (własne wyjątki)

#### 2.2 Input Validation

**Zadanie:** FluentValidation dla wszystkich DTO.

**Implementacja:**
```csharp
// Aegis.Backend/Validators/RegisterRequestValidator.cs
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Username can only contain alphanumeric, underscore, and dash");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])")
            .WithMessage("Password must contain uppercase, lowercase, number and special char");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
```

**Pliki do stworzenia:**
- `Aegis.Backend/Validators/` (wszystkie validatory)
- NuGet: `FluentValidation.AspNetCore`

#### 2.3 Structured Logging

**Zadanie:** Spójne strukturalne logowanie.

**Implementacja:**
```csharp
// appsettings.json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/aegis-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

#### 2.4 Health Checks

**Zadanie:** Comprehensive health checks.

**Implementacja:**
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AegisDbContext>("database")
    .AddSignalRHub("signalr-hub", "/hubs/messages")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<StorageHealthCheck>("file-storage");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

**Pliki do stworzenia:**
- `Aegis.Backend/HealthChecks/RedisHealthCheck.cs`
- `Aegis.Backend/HealthChecks/StorageHealthCheck.cs`
- NuGet: `AspNetCore.HealthChecks.*`

#### 2.5 Unit Tests

**Zadanie:** Comprehensive test coverage (min. 80%).

**Struktura:**
```
tests/
├── Aegis.Core.Tests/
│   ├── Cryptography/
│   │   ├── SignalSessionManagerTests.cs
│   │   ├── EncryptionServiceTests.cs
│   │   └── KeyDerivationTests.cs
│   ├── Security/
│   │   ├── RootDetectionTests.cs
│   │   ├── AntiDebugTests.cs
│   │   └── DuressPinManagerTests.cs
│   └── Models/
│       └── MessageTests.cs
│
├── Aegis.Data.Tests/
│   ├── Repositories/
│   │   ├── MessageRepositoryTests.cs
│   │   └── UserRepositoryTests.cs
│   └── Context/
│       └── AegisDbContextTests.cs
│
└── Aegis.Backend.Tests/
    ├── Controllers/
    │   ├── AuthControllerTests.cs
    │   └── MessagesControllerTests.cs
    ├── Hubs/
    │   └── MessageHubTests.cs
    └── Integration/
        ├── AuthFlowTests.cs
        └── MessageFlowTests.cs
```

**Technologie:**
- xUnit
- Moq
- FluentAssertions
- Microsoft.AspNetCore.Mvc.Testing (integration tests)

#### 2.6 CI/CD Pipeline

**Zadanie:** GitHub Actions workflow.

**Plik:** `.github/workflows/dotnet.yml`

```yaml
name: .NET Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore dotnet/Aegis.sln

    - name: Build
      run: dotnet build dotnet/Aegis.sln --no-restore

    - name: Test
      run: dotnet test dotnet/Aegis.sln --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3

    - name: Security scan
      run: dotnet tool install --global security-scan
      run: security-scan dotnet/Aegis.sln
```

---

### FAZA 3: 🚀 NOWE FUNKCJONALNOŚCI (4-6 tygodni)

#### 3.1 Disappearing Messages

**Zadanie:** Automatyczne usuwanie wiadomości po określonym czasie.

**Implementacja:**
```csharp
// Aegis.Core/Models/Message.cs
public class Message
{
    // ... existing properties
    public bool IsDisappearing { get; set; }
    public int DisappearAfterSeconds { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DisappearAt { get; set; }
}

// Aegis.Backend/Services/MessageCleanupService.cs
public class MessageCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DeleteExpiredMessagesAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task DeleteExpiredMessagesAsync()
    {
        var expiredMessages = await _context.Messages
            .Where(m => m.IsDisappearing &&
                       m.DisappearAt != null &&
                       m.DisappearAt <= DateTime.UtcNow)
            .ToListAsync();

        _context.Messages.RemoveRange(expiredMessages);
        await _context.SaveChangesAsync();
    }
}
```

**Pliki do stworzenia:**
- `Aegis.Backend/Services/MessageCleanupService.cs`
- Migration dla nowych kolumn

#### 3.2 Message Reactions

**Zadanie:** Reakcje emoji na wiadomości.

**Implementacja:**
```csharp
// Aegis.Core/Models/MessageReaction.cs
public class MessageReaction
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } // ❤️, 👍, 😂, etc.
    public DateTime CreatedAt { get; set; }
}

// SignalR MessageHub
public async Task ReactToMessage(string messageId, string emoji)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var reaction = new MessageReaction
    {
        MessageId = Guid.Parse(messageId),
        UserId = Guid.Parse(userId),
        Emoji = emoji
    };

    await _reactionRepository.AddAsync(reaction);

    // Notify sender
    await Clients.Group(senderId).SendAsync("MessageReaction", new
    {
        messageId,
        userId,
        emoji
    });
}
```

**Pliki do stworzenia:**
- `Aegis.Core/Models/MessageReaction.cs`
- `Aegis.Data/Entities/MessageReactionEntity.cs`
- `Aegis.Data/Repositories/ReactionRepository.cs`
- Migration

#### 3.3 Message Search

**Zadanie:** Full-text search wiadomości.

**Implementacja:**
```csharp
// SQL Server Full-Text Search
// Migration
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        CREATE FULLTEXT CATALOG AegisCatalog AS DEFAULT;

        CREATE FULLTEXT INDEX ON Messages(EncryptedContent)
        KEY INDEX PK_Messages
        WITH STOPLIST = SYSTEM;
    ");
}

// Repository
public async Task<List<Message>> SearchMessagesAsync(
    Guid userId,
    string searchQuery,
    int limit = 50)
{
    // Note: Search tylko na odszyfrowanych wiadomościach po stronie klienta
    // Lub przechowywanie zaszyfrowanego indeksu

    var messages = await _context.Messages
        .Where(m => (m.SenderId == userId || m.ReceiverId == userId))
        .Where(m => EF.Functions.FreeText(m.EncryptedContent, searchQuery))
        .OrderByDescending(m => m.Timestamp)
        .Take(limit)
        .ToListAsync();

    return messages.Select(MapToModel).ToList();
}
```

**Uwaga:** Search na zaszyfrowanych danych jest trudny. Rozważyć:
1. Lokalne przechowywanie niezaszyfrowanego indeksu (Windows: SQLite, Android: Room)
2. Searchable Encryption (SEE)
3. Homomorphic encryption (kosztowne)

#### 3.4 Push Notifications (Android)

**Zadanie:** Firebase Cloud Messaging dla notyfikacji.

**Implementacja:**
```csharp
// NuGet: FirebaseAdmin
// Aegis.Backend/Services/PushNotificationService.cs
public class PushNotificationService
{
    private readonly FirebaseMessaging _messaging;

    public async Task SendMessageNotificationAsync(
        string deviceToken,
        string senderName,
        bool showPreview = false)
    {
        var message = new Message
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = senderName,
                Body = showPreview ? "New message" : "🔒 Encrypted message"
            },
            Data = new Dictionary<string, string>
            {
                { "type", "new_message" },
                { "encrypted", "true" }
            }
        };

        await _messaging.SendAsync(message);
    }
}

// Android: Aegis.Android/Platforms/Android/FirebaseMessagingService.cs
[Service(Exported = true)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class AegisFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        // Display notification
        var notificationManager = NotificationManager.FromContext(this);
        var notification = new NotificationCompat.Builder(this, "aegis_channel")
            .SetContentTitle(message.GetNotification().Title)
            .SetContentText(message.GetNotification().Body)
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetPriority(NotificationCompat.PriorityHigh)
            .Build();

        notificationManager.Notify(1, notification);
    }
}
```

**Pliki do stworzenia:**
- `Aegis.Backend/Services/PushNotificationService.cs`
- `Aegis.Android/Platforms/Android/FirebaseMessagingService.cs`
- `google-services.json` (Android)
- Firebase project setup

#### 3.5 Voice/Video Calls

**Zadanie:** WebRTC dla połączeń głosowych/wideo.

**Implementacja:**
```csharp
// SignalR Hub - Signaling
public async Task InitiateCall(string recipientId, string sdpOffer)
{
    var callerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    await Clients.Group(recipientId).SendAsync("IncomingCall", new
    {
        callerId,
        sdpOffer,
        callType = "video"
    });
}

public async Task AnswerCall(string callerId, string sdpAnswer)
{
    await Clients.Group(callerId).SendAsync("CallAnswered", new
    {
        sdpAnswer
    });
}

public async Task SendIceCandidate(string recipientId, string candidate)
{
    await Clients.Group(recipientId).SendAsync("IceCandidate", new
    {
        candidate
    });
}
```

**Klient (Desktop/Android):**
- WebRTC library: `WebRTC.NET` (Desktop), `Xamarin.WebRTC` (Android)
- STUN/TURN server (coturn, Twilio, etc.)
- End-to-end encryption audio/video streams (SRTP)

**Pliki do stworzenia:**
- `Aegis.Backend/Hubs/CallHub.cs`
- `Aegis.Desktop/Services/WebRtcService.cs`
- `Aegis.Android/Services/WebRtcService.cs`
- TURN server configuration

#### 3.6 Backup & Restore

**Zadanie:** Szyfrowane kopie zapasowe.

**Implementacja:**
```csharp
// Aegis.Core/Services/BackupService.cs
public class BackupService
{
    public async Task<byte[]> CreateBackupAsync(
        Guid userId,
        string backupPassword)
    {
        // Pobierz wszystkie dane użytkownika
        var messages = await _messageRepo.GetAllUserMessagesAsync(userId);
        var contacts = await _userRepo.GetContactsAsync(userId);
        var settings = await _settingsRepo.GetUserSettingsAsync(userId);

        var backup = new BackupData
        {
            Messages = messages,
            Contacts = contacts,
            Settings = settings,
            CreatedAt = DateTime.UtcNow
        };

        // Serializuj do JSON
        var json = JsonSerializer.Serialize(backup);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        // Zaszyfruj używając hasła backupu
        var (salt, key) = DeriveBackupKey(backupPassword);
        var encrypted = await _encryptionService.EncryptAsync(jsonBytes, key);

        // Struktura: [Version(4)][Salt(32)][EncryptedData]
        var result = new byte[4 + salt.Length + encrypted.Length];
        BitConverter.GetBytes(1).CopyTo(result, 0); // Version
        salt.CopyTo(result, 4);
        encrypted.CopyTo(result, 4 + salt.Length);

        return result;
    }

    public async Task RestoreBackupAsync(
        byte[] backupData,
        string backupPassword)
    {
        // Parse backup
        var version = BitConverter.ToInt32(backupData, 0);
        var salt = backupData[4..36];
        var encrypted = backupData[36..];

        // Derive key
        var key = KeyDerivation.DeriveKeyPBKDF2(backupPassword, salt);

        // Decrypt
        var decrypted = await _encryptionService.DecryptAsync(encrypted, key);
        var json = Encoding.UTF8.GetString(decrypted);
        var backup = JsonSerializer.Deserialize<BackupData>(json);

        // Restore data
        await RestoreMessagesAsync(backup.Messages);
        await RestoreContactsAsync(backup.Contacts);
        await RestoreSettingsAsync(backup.Settings);
    }
}
```

**Storage:**
- Local: File system
- Cloud: Azure Blob Storage, AWS S3, Google Cloud Storage
- Automatic scheduled backups

**Pliki do stworzenia:**
- `Aegis.Core/Services/BackupService.cs`
- `Aegis.Core/Models/BackupData.cs`
- `Aegis.Desktop/Views/BackupView.xaml`
- `Aegis.Android/Pages/BackupPage.xaml`

---

### FAZA 4: 🌐 ZAAWANSOWANE FUNKCJONALNOŚCI (6-8 tygodni)

#### 4.1 Multi-Device Sync

**Zadanie:** Synchronizacja między wieloma urządzeniami.

**Architektura:**
```
User -> Primary Device (deviceId: 1)
     -> Secondary Device (deviceId: 2)
     -> Tablet (deviceId: 3)
```

**Implementacja:**
```csharp
// Aegis.Core/Models/Device.cs
public class Device
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public uint DeviceId { get; set; }
    public string DeviceName { get; set; }
    public DeviceType Type { get; set; } // Desktop, Android, iOS
    public string PublicIdentityKey { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsPrimary { get; set; }
}

// SignalR Hub
public async Task SyncMessages(uint fromDeviceId, DateTime since)
{
    var userId = GetCurrentUserId();
    var devices = await _deviceRepo.GetUserDevicesAsync(userId);

    var messages = await _messageRepo.GetMessagesSinceAsync(userId, since);

    // Encrypt messages for each device
    foreach (var device in devices.Where(d => d.DeviceId != fromDeviceId))
    {
        var encryptedMessages = await EncryptForDevice(messages, device);
        await Clients.Group($"user_{userId}_device_{device.DeviceId}")
            .SendAsync("SyncMessages", encryptedMessages);
    }
}
```

**Pliki do stworzenia:**
- `Aegis.Core/Models/Device.cs`
- `Aegis.Data/Repositories/DeviceRepository.cs`
- `Aegis.Backend/Services/DeviceSyncService.cs`
- Migration

#### 4.2 Contact Discovery (Private)

**Zadanie:** Prywatne odkrywanie kontaktów bez ujawniania numerów telefonu.

**Metoda: Private Set Intersection (PSI)**

**Implementacja:**
```csharp
// Aegis.Backend/Services/ContactDiscoveryService.cs
public class ContactDiscoveryService
{
    // Użytkownik wysyła hashe numerów telefonu
    public async Task<List<Guid>> DiscoverContactsAsync(
        List<string> phoneNumberHashes)
    {
        var discoveredUsers = new List<Guid>();

        foreach (var hash in phoneNumberHashes)
        {
            // Szukaj w bazie użytkowników z matching hash
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    SHA256.HashData(Encoding.UTF8.GetBytes(u.PhoneNumber))
                    == Convert.FromBase64String(hash));

            if (user != null)
                discoveredUsers.Add(user.Id);
        }

        return discoveredUsers;
    }
}
```

**Zaawansowana metoda: SGX Enclave**
- Intel SGX dla trusted execution
- Server nie widzi plain-text numerów telefonu
- Wymaga sprzętowego wsparcia SGX

#### 4.3 Offline Messages Queue

**Zadanie:** Kolejkowanie wiadomości gdy odbiorca offline.

**Implementacja:**
```csharp
// Aegis.Backend/Services/OfflineMessageQueue.cs
public class OfflineMessageQueue
{
    private readonly IDistributedCache _cache; // Redis

    public async Task QueueMessageAsync(Guid recipientId, Message message)
    {
        var queueKey = $"offline_queue:{recipientId}";
        var serialized = JsonSerializer.Serialize(message);

        await _cache.SetStringAsync(
            queueKey,
            serialized,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            });
    }

    public async Task<List<Message>> GetQueuedMessagesAsync(Guid userId)
    {
        var queueKey = $"offline_queue:{userId}";
        var serialized = await _cache.GetStringAsync(queueKey);

        if (string.IsNullOrEmpty(serialized))
            return new List<Message>();

        var messages = JsonSerializer.Deserialize<List<Message>>(serialized);
        await _cache.RemoveAsync(queueKey);

        return messages;
    }
}
```

**Pliki do stworzenia:**
- `Aegis.Backend/Services/OfflineMessageQueue.cs`
- Redis configuration
- NuGet: `Microsoft.Extensions.Caching.StackExchangeRedis`

#### 4.4 Message Forwarding & Quotes

**Zadanie:** Przekazywanie i cytowanie wiadomości.

**Implementacja:**
```csharp
// Aegis.Core/Models/Message.cs
public class Message
{
    // ... existing
    public Guid? QuotedMessageId { get; set; }
    public bool IsForwarded { get; set; }
    public Guid? OriginalSenderId { get; set; }
}

// SignalR Hub
public async Task ForwardMessage(string originalMessageId, string recipientId)
{
    var senderId = GetCurrentUserId();
    var originalMessage = await _messageRepo.GetByIdAsync(
        Guid.Parse(originalMessageId));

    // Decrypt original message
    var plaintext = await _signalProtocol.DecryptMessageAsync(...);

    // Re-encrypt for new recipient
    var encrypted = await _signalProtocol.EncryptMessageAsync(
        Guid.Parse(recipientId), plaintext);

    var forwardedMessage = new Message
    {
        SenderId = Guid.Parse(senderId),
        ReceiverId = Guid.Parse(recipientId),
        EncryptedContent = encrypted,
        IsForwarded = true,
        OriginalSenderId = originalMessage.SenderId
    };

    await _messageRepo.InsertAsync(forwardedMessage);
    await Clients.Group(recipientId).SendAsync("ReceiveMessage", forwardedMessage);
}
```

#### 4.5 Groups - Advanced Features

**Zadanie:** Rozbudowa funkcjonalności grupowych.

**Funkcje:**
- Admin controls (mute, kick, ban)
- Invite links
- Group permissions
- Announcements only mode
- Sender key rotation

**Implementacja:**
```csharp
// Aegis.Core/Models/GroupInviteLink.cs
public class GroupInviteLink
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InviteCode { get; set; } // Random string
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
}

// API Endpoint
[HttpPost("groups/{groupId}/invite-link")]
public async Task<IActionResult> CreateInviteLink(
    Guid groupId,
    [FromBody] CreateInviteLinkRequest request)
{
    var userId = GetCurrentUserId();

    // Check if user is admin
    var member = await _groupRepo.GetGroupMemberAsync(groupId, userId);
    if (member.Role != GroupRole.Admin && member.Role != GroupRole.Owner)
        return Forbid();

    var inviteLink = new GroupInviteLink
    {
        GroupId = groupId,
        InviteCode = GenerateInviteCode(),
        ExpiresAt = request.ExpiresInHours.HasValue
            ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value)
            : null,
        MaxUses = request.MaxUses
    };

    await _groupRepo.AddInviteLinkAsync(inviteLink);

    return Ok(new { inviteUrl = $"https://aegis.app/join/{inviteLink.InviteCode}" });
}
```

---

## 📊 PRIORYTETYZACJA

### Sprint 1 (2 tygodnie) - KRYTYCZNE
- ✅ Persistent Signal Protocol Store
- ✅ Secure JWT Configuration
- ✅ Rate Limiting
- ✅ CORS Hardening

### Sprint 2 (2 tygodnie) - WYSOKIE
- ✅ Global Error Handling
- ✅ Input Validation
- ✅ Health Checks
- ✅ Unit Tests (Core)

### Sprint 3 (2 tygodnie) - ŚREDNIE
- ✅ Disappearing Messages
- ✅ Message Reactions
- ✅ Push Notifications

### Sprint 4 (2 tygodnie) - ZAAWANSOWANE
- ✅ Message Search
- ✅ Backup & Restore

### Sprint 5+ (4+ tygodnie) - PRZYSZŁOŚĆ
- Voice/Video Calls
- Multi-Device Sync
- Contact Discovery

---

## 📈 METRYKI SUKCESU

| Kategoria | Metric | Target |
|-----------|--------|--------|
| **Bezpieczeństwo** | Znalezione CVE | 0 |
| **Kod** | Test Coverage | > 80% |
| **Kod** | Code Smells (SonarQube) | < 50 |
| **Performance** | API Response Time (p95) | < 200ms |
| **Performance** | Message Delivery Time | < 1s |
| **Reliability** | Uptime | > 99.9% |
| **User Experience** | Crash-free Rate | > 99.5% |

---

## 🔧 NARZĘDZIA

### Security
- **SonarQube** - static code analysis
- **OWASP Dependency Check** - vulnerable dependencies
- **Snyk** - container security
- **ZAP** - penetration testing

### Code Quality
- **StyleCop** - C# code style
- **Roslynator** - code analyzers
- **EditorConfig** - consistent coding style

### Monitoring
- **Seq** - structured logging
- **Application Insights** - APM
- **Prometheus + Grafana** - metrics
- **Sentry** - error tracking

---

## 📝 PODSUMOWANIE

Ten plan rozwoju zapewnia:
1. **Bezpieczeństwo** - Naprawa krytycznych luk + proaktywna ochrona
2. **Jakość** - Testy, CI/CD, monitoring
3. **Funkcjonalność** - Konkurencyjne features (Signal, WhatsApp level)
4. **Skalowalność** - Redis, proper architecture
5. **User Experience** - Disappearing messages, reactions, search

**Szacowany czas pełnej implementacji:** 16-20 tygodni (4-5 miesięcy)

**Rekomendacja:** Rozpocznij od Sprint 1 (krytyczne bezpieczeństwo), a następnie iteracyjnie dodawaj funkcjonalności zgodnie z feedback użytkowników.
