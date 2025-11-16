# Aegis Security Module

Kompletny moduł bezpieczeństwa dla Aegis Messenger, zapewniający:
- **Security Audit Logging** - kompletny audit trail wszystkich zdarzeń
- **Rate Limiting** - ochrona przed abuse i brute force (in-memory + Redis distributed)
- **Email/Webhook Alerting** - automatyczne powiadomienia dla Critical events
- **Windows DPAPI Key Storage** - bezpieczne przechowywanie kluczy
- **Automatic Middleware** - automatyczne logowanie i rate limiting dla HTTP requests
- **Domain Event Handlers** - automatyczne logowanie domain events

## Architektura

```
Security/
├── Domain/              # Encje, enums, repositories
│   ├── Entities/
│   │   └── SecurityAuditLog.cs
│   ├── Enums/
│   │   ├── SecurityEventType.cs (40+ typów)
│   │   └── SecurityEventSeverity.cs (5 poziomów)
│   └── Repositories/
│       └── ISecurityAuditRepository.cs
├── Application/         # Serwisy, event handlers, alerting
│   ├── Services/
│   │   ├── ISecurityAuditService.cs
│   │   └── SecurityAuditService.cs
│   ├── Alerting/
│   │   ├── IAlertingService.cs
│   │   ├── SecurityAlert.cs
│   │   └── AlertingOptions.cs
│   └── EventHandlers/
│       └── DomainEventAuditHandler.cs
├── Infrastructure/      # Persistence, DbContext, alerting implementation
│   ├── Persistence/
│   │   ├── SecurityDbContext.cs
│   │   ├── Configurations/
│   │   └── Repositories/
│   ├── Alerting/
│   │   ├── EmailAlertingService.cs (MailKit/SMTP)
│   │   ├── WebhookAlertingService.cs (Slack/Discord/Teams/Generic)
│   │   └── CompositeAlertingService.cs
│   └── DependencyInjection.cs
└── API/                 # Controllers, middleware, services
    ├── Middleware/
    │   ├── RateLimitMiddleware.cs
    │   └── SecurityAuditMiddleware.cs
    ├── Services/
    │   ├── IRateLimitingService.cs
    │   ├── RateLimitingService.cs (in-memory)
    │   └── RedisRateLimitingService.cs (distributed)
    ├── Extensions/
    │   ├── HttpContextSecurityExtensions.cs
    │   └── ControllerBaseSecurityExtensions.cs
    └── DependencyInjection.cs
```

## Funkcje

### 1. Security Audit Log

Automatyczne logowanie wszystkich zdarzeń bezpieczeństwa:

**40+ Typów Zdarzeń:**
- **Authentication**: Login, Logout, Failed Login
- **Account Management**: Created, Deleted, Password Changed
- **Cryptography**: Key Generated, Rotated, Session Initialized
- **Privacy**: Settings Changed, Disappearing Messages
- **Messages**: Sent, Deleted, Expired
- **Groups**: Created, User Joined/Left, Promoted/Demoted
- **Files**: Uploaded, Downloaded, Deleted
- **Security**: Rate Limit Exceeded, Suspicious Activity, Unauthorized Access

**5 Poziomów Severity:**
- **Info** - normalne operacje
- **Low** - drobne problemy
- **Medium** - wymaga przeglądu
- **High** - wymaga uwagi
- **Critical** - natychmiastowa akcja

**Przykład użycia:**

```csharp
// Inject service
private readonly ISecurityAuditService _auditService;

// Log success
await _auditService.LogSuccessAsync(
    SecurityEventType.MessageSent,
    userId: currentUserId,
    ipAddress: "192.168.1.1",
    userAgent: "Mozilla/5.0...",
    details: "Sent message to user X"
);

// Log failure
await _auditService.LogFailureAsync(
    SecurityEventType.LoginFailed,
    errorMessage: "Invalid credentials",
    userId: attemptedUserId,
    ipAddress: "192.168.1.1"
);

// Check for excessive failed logins (brute force detection)
var hasExcessiveFailures = await _auditService.HasExcessiveFailedLoginsAsync(
    userId: userId,
    ipAddress: "192.168.1.1"
);
// Returns true if:
// - 5+ failed logins in last 15 minutes, OR
// - 20+ failed logins in last 24 hours
```

### 2. Rate Limiting

Automatyczna ochrona przed abuse z predefiniowanymi limitami. Obsługuje dwie implementacje:

**Implementacje:**
- **In-Memory** - prosty, szybki, dla single-instance deployments
- **Redis** - distributed, atomiczny, dla multi-instance deployments (klastry, load balancing)

**13 Operacji:**
```csharp
login:               5 requests / 15 minutes
register:            3 requests / hour
refresh_token:      10 requests / 5 minutes
send_message:       60 requests / minute
send_group_message: 30 requests / minute
create_group:        5 requests / hour
invite_to_group:    20 requests / minute
upload_file:        10 requests / 5 minutes
download_file:      30 requests / minute
add_contact:        20 requests / 10 minutes
block_user:         10 requests / 10 minutes
update_profile:      5 requests / 5 minutes
default:            30 requests / 5 minutes
```

**Algorytm:** Sliding Window - precyzyjne limitowanie w czasie

**Konfiguracja Redis (opcjonalna):**

Aby włączyć distributed rate limiting z Redis, dodaj connection string w `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false,connectTimeout=5000,syncTimeout=5000"
  }
}
```

**Automatyczny fallback:**
- Jeśli Redis jest skonfigurowany → używa `RedisRateLimitingService`
- Jeśli połączenie się nie powiedzie → fallback do in-memory
- Jeśli Redis nie jest skonfigurowany → używa in-memory

**Redis Implementation Details:**
- Używa **Lua scripts** dla atomicznych operacji (brak race conditions)
- **Sorted Sets (ZSET)** do przechowywania timestampów
- Automatyczne **expiration** starych wpisów
- **Fail-open** strategy - zezwala na requesty gdy Redis jest niedostępny
- Format kluczy: `ratelimit:{operation}:{key}`

**Przykład użycia:**

```csharp
// Inject service
private readonly IRateLimitingService _rateLimiting;

// Check if request is allowed
var isAllowed = _rateLimiting.AllowRequest(
    key: $"user:{userId}",
    operation: "send_message"
);

if (!isAllowed)
{
    // Rate limit exceeded
    var remaining = _rateLimiting.GetRemainingRequests(key, operation);
    var resetTime = _rateLimiting.GetTimeUntilReset(key, operation);

    return StatusCode(429, new
    {
        error = "Rate limit exceeded",
        retryAfter = (int)resetTime.TotalSeconds,
        remaining = remaining
    });
}

// Continue processing...
```

### 3. Middleware - Automatic Protection

Middleware automatycznie sprawdza rate limity i loguje wszystkie requesty:

**Program.cs:**
```csharp
app.UseSecurityAudit();    // Loguje wszystkie API requests
app.UseRateLimiting();     // Sprawdza rate limits

app.UseAuthentication();
app.UseAuthorization();
```

**Co middleware robi:**

**RateLimitMiddleware:**
- Automatycznie mapuje routes do operacji
- Sprawdza rate limits przed przetworzeniem requestu
- Zwraca 429 Too Many Requests przy przekroczeniu
- Dodaje headers: `X-RateLimit-Remaining`, `Retry-After`
- Loguje do audit log przy przekroczeniu limitu

**SecurityAuditMiddleware:**
- Loguje wszystkie API requests (POST/PUT/PATCH/DELETE)
- Pomija GET requests (zbyt głośne)
- Loguje wszystkie błędy (4xx, 5xx)
- Automatycznie określa event type z route
- Mierzy czas wykonania requestu

### 4. Domain Event Handlers

Automatyczne logowanie domain events do audit log:

**DomainEventAuditHandler** obsługuje:
- `UserRegisteredEvent` → `SecurityEventType.AccountCreated`
- `UserLoggedInEvent` → `SecurityEventType.LoginSuccess`
- `MessageSentEvent` → `SecurityEventType.MessageSent`
- `MessageDeliveredEvent` → (pomijane, zbyt głośne)
- `MessageReadEvent` → (pomijane, zbyt głośne)

**Jak to działa:**

1. Domain entity publikuje event:
   ```csharp
   user.RaiseDomainEvent(new UserLoggedInEvent(userId, DateTime.UtcNow));
   ```

2. Event jest publikowany przez EventBus:
   ```csharp
   await _eventBus.PublishManyAsync(user.DomainEvents, cancellationToken);
   ```

3. `DomainEventAuditHandler` automatycznie przechwytuje i loguje:
   ```csharp
   await _auditService.LogSuccessAsync(
       SecurityEventType.LoginSuccess,
       userId,
       details: "User logged in successfully"
   );
   ```

**Korzyści:**
- ✅ Zero boilerplate - działa automatycznie
- ✅ Separation of Concerns - logika biznesowa nie zanieczyszczona loggingiem
- ✅ Kompletny audit trail - wszystkie domain events logowane
- ✅ Łatwa konfiguracja - dodaj nowy handler dla nowego eventu

### 5. Email/Webhook Alerting

Automatyczne powiadomienia dla zdarzeń Critical i High severity poprzez email i webhooks.

**Kiedy wysyłane są alerty:**
- Zdarzenia z severity **High** lub **Critical**
- Nieudane próby z severity **Medium** lub wyższym
- Automatycznie wywołane przez `SecurityAuditService.LogSuccessAsync()` / `LogFailureAsync()`

**Obsługiwane kanały:**

**Email (via MailKit/SMTP):**
- Profesjonalne HTML i text wersje emaili
- Kolory zależne od severity (Critical=Red, High=Orange, etc.)
- Wszystkie szczegóły zdarzenia w czytelnym formacie
- Automatyczne retry przy błędach SMTP

**Webhooks:**
- **Slack** - formatted attachments z polami i kolorami
- **Discord** - rich embeds z kolorami i ikonami
- **Microsoft Teams** - MessageCard format
- **Generic** - czysty JSON dla custom endpoints
- Retry logic z exponential backoff (3 próby)
- Configurable timeout i custom headers

**Konfiguracja w appsettings.json:**

```json
{
  "Security": {
    "Alerting": {
      "Enabled": true,
      "Email": {
        "Enabled": true,
        "SmtpServer": "smtp.gmail.com",
        "SmtpPort": 587,
        "UseSsl": true,
        "Username": "your-email@gmail.com",
        "Password": "your-app-password",
        "FromAddress": "security@aegismessenger.com",
        "FromName": "Aegis Messenger Security",
        "ToAddresses": [
          "admin@example.com",
          "security-team@example.com"
        ]
      },
      "Webhooks": [
        {
          "Name": "Slack Production",
          "Url": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
          "Type": "Slack",
          "Headers": {},
          "TimeoutSeconds": 10,
          "MaxRetries": 3
        },
        {
          "Name": "Discord Security",
          "Url": "https://discord.com/api/webhooks/YOUR/WEBHOOK",
          "Type": "Discord",
          "Headers": {},
          "TimeoutSeconds": 10,
          "MaxRetries": 3
        },
        {
          "Name": "Custom Endpoint",
          "Url": "https://your-api.com/security-alerts",
          "Type": "Generic",
          "Headers": {
            "Authorization": "Bearer YOUR_TOKEN",
            "X-Custom-Header": "value"
          },
          "TimeoutSeconds": 5,
          "MaxRetries": 2
        }
      ]
    }
  }
}
```

**Email Setup (Gmail example):**
1. Włącz 2-Factor Authentication w Gmail
2. Wygeneruj App Password: https://myaccount.google.com/apppasswords
3. Użyj App Password jako `Password` w konfiguracji

**Slack Webhook Setup:**
1. Wejdź do Slack App Directory → Incoming Webhooks
2. Wybierz kanał i utwórz webhook
3. Skopiuj webhook URL do konfiguracji

**Discord Webhook Setup:**
1. Server Settings → Integrations → Webhooks
2. Create Webhook i wybierz kanał
3. Copy Webhook URL

**Microsoft Teams Webhook Setup:**
1. Teams channel → Connectors → Incoming Webhook
2. Configure i skopiuj URL
3. Ustaw `Type: "MicrosoftTeams"`

**Fire-and-Forget Delivery:**
Alerty są wysyłane asynchronicznie (Task.Run) aby nie blokować request pipeline:
- Błędy wysyłania są logowane ale nie przerywają requestu
- Retry logic automatycznie powtarza przy przejściowych błędach
- Fail-safe - aplikacja działa nawet gdy alerting nie działa

**Przykładowe zdarzenia generujące alerty:**
- ❌ Failed login attempts (High severity)
- 🔑 Password changed (Critical severity)
- 🔐 Key rotation (Critical severity)
- 🗑️ Account deleted (Critical severity)
- ⚠️ Rate limit exceeded (Medium severity - tylko przy failure)
- 🚨 Suspicious activity detected (Critical severity)
- 🚫 Unauthorized access attempts (High severity)

### 6. Helper Extensions

**HttpContextSecurityExtensions** - łatwy dostęp do informacji o requestcie:

```csharp
// W middleware/controllerze
var userId = context.GetUserId();
var ipAddress = context.GetIpAddress();
var userAgent = context.GetUserAgent();
var isAuthenticated = context.IsAuthenticated();
var username = context.GetUsername();
var email = context.GetEmail();
var roles = context.GetRoles();
var fingerprint = context.GetRequestFingerprint();
```

**ControllerBaseSecurityExtensions** - łatwe logowanie w controllerach:

```csharp
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly ISecurityAuditService _auditService;

    [HttpPost]
    public async Task<IActionResult> SendMessage(SendMessageRequest request)
    {
        // ... send message logic ...

        // Log security event
        await this.LogSecuritySuccessAsync(
            _auditService,
            SecurityEventType.MessageSent,
            details: $"Sent message to user {request.RecipientId}",
            relatedEntityId: messageId,
            relatedEntityType: "Message"
        );

        return Ok(result);
    }

    // Get current user ID
    var userId = this.GetCurrentUserIdOrThrow();  // Throws if not authenticated
}
```

### 7. Queries - Przeglądanie Audit Logs

**ISecurityAuditRepository** zapewnia wydajne queries:

```csharp
// Historia aktywności użytkownika
var userLogs = await _repository.GetUserLogsAsync(
    userId,
    limit: 100,
    from: DateTime.UtcNow.AddDays(-7)
);

// Wszystkie nieudane operacje
var failedEvents = await _repository.GetFailedEventsAsync(
    limit: 50,
    from: DateTime.UtcNow.AddHours(-1)
);

// Zdarzenia wysokiej wagi
var highSeverity = await _repository.GetHighSeverityEventsAsync(
    limit: 20
);

// Zdarzenia wymagające alertów
var alerts = await _repository.GetAlertableEventsAsync(
    from: DateTime.UtcNow.AddHours(-24)
);

// Liczba nieudanych logowań
var failedLogins = await _repository.GetFailedLoginCountAsync(
    userId: userId,
    timeWindow: TimeSpan.FromHours(1)
);

// GDPR - usuwanie starych logów
var deleted = await _repository.DeleteOldLogsAsync(
    olderThan: DateTime.UtcNow.AddMonths(-6)
);
```

## Konfiguracja

### 1. Database

Dodaj connection string w `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SecurityDatabase": "Server=localhost;Database=AegisMessenger_Security;..."
  }
}
```

### 2. Dependency Injection

W `Program.cs`:

```csharp
// Dodaj moduł Security
builder.Services.AddSecurityModule(builder.Configuration);

// Skonfiguruj middleware pipeline
app.UseSecurityAudit();    // Przed authentication
app.UseRateLimiting();     // Przed authentication
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Migracje

Utwórz migracje dla SecurityDbContext:

```bash
dotnet ef migrations add InitialSecuritySchema \
    --project Aegis.Modules.Security.Infrastructure \
    --startup-project Aegis.Host.API \
    --context SecurityDbContext \
    --output-dir Persistence/Migrations

dotnet ef database update \
    --project Aegis.Modules.Security.Infrastructure \
    --startup-project Aegis.Host.API \
    --context SecurityDbContext
```

## Performance

### Indeksy Bazy Danych

SecurityAuditLog ma 7 indeksów dla wydajnych queries:

```sql
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_EventType ON AuditLogs(EventType);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_AuditLogs_Severity ON AuditLogs(Severity);
CREATE INDEX IX_AuditLogs_IsSuccessful ON AuditLogs(IsSuccessful);
CREATE INDEX IX_AuditLogs_IpAddress_Timestamp ON AuditLogs(IpAddress, Timestamp);
CREATE INDEX IX_AuditLogs_UserId_EventType_Timestamp ON AuditLogs(UserId, EventType, Timestamp);
```

### Rate Limiting

- **In-Memory** implementation dla single-instance deployment
- **Redis-ready** - łatwa migracja do Redis dla distributed systems
- Automatyczne czyszczenie expired entries
- O(1) complexity dla check operations

### Audit Logging

- **Asynchroniczne** zapisy do bazy
- **Scoped** lifetime - jeden DbContext per request
- **Background processing** - nie blokuje głównego flow

## Security Best Practices

### 1. IP Address Privacy

Middleware uwzględnia proxy/load balancer headers:
- `X-Forwarded-For`
- `X-Real-IP`
- `CF-Connecting-IP` (Cloudflare)

### 2. Rate Limit Keys

- **Authenticated users**: `user:{userId}`
- **Anonymous users**: `ip:{ipAddress}`

Zapobiega obejściu limitów poprzez zmianę IP dla zalogowanych użytkowników.

### 3. Sensitive Data

**NIE loguj** do audit log:
- ❌ Haseł (nawet zahashowanych)
- ❌ Tokenów (JWT, refresh tokens)
- ❌ Kluczy kryptograficznych
- ❌ Treści wiadomości (tylko metadane)
- ❌ Danych osobowych (GDPR)

**TAK loguj**:
- ✅ User IDs
- ✅ Event types
- ✅ Timestamps
- ✅ IP addresses (z uwagą na GDPR)
- ✅ Success/failure status
- ✅ Error messages (bez stack traces w produkcji)

### 4. GDPR Compliance

```csharp
// Regularnie usuwaj stare logi
public class AuditLogCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Usuń logi starsze niż 6 miesięcy
            var olderThan = DateTime.UtcNow.AddMonths(-6);
            await _repository.DeleteOldLogsAsync(olderThan);

            // Czekaj 24h
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

## Monitoring & Alerting

### Real-time Alerts

```csharp
// W SecurityAuditService
if (auditLog.ShouldAlert())
{
    _logger.LogError("SECURITY ALERT: {EventType} - {Details}",
        eventType, details);

    // TODO: Wysłać email/webhook
    // await _alertingService.SendAlertAsync(auditLog);
}
```

### Dashboard Queries

```sql
-- Top 10 użytkowników z najwięcej nieudanymi loginami (ostatnie 24h)
SELECT UserId, COUNT(*) as FailedAttempts
FROM AuditLogs
WHERE EventType = 'LoginFailed'
  AND Timestamp >= DATEADD(hour, -24, GETUTCDATE())
GROUP BY UserId
ORDER BY FailedAttempts DESC
LIMIT 10;

-- Zdarzenia Critical severity (ostatnie 7 dni)
SELECT *
FROM AuditLogs
WHERE Severity = 'Critical'
  AND Timestamp >= DATEADD(day, -7, GETUTCDATE())
ORDER BY Timestamp DESC;

-- Rate limit violations per user
SELECT UserId, COUNT(*) as Violations
FROM AuditLogs
WHERE EventType = 'RateLimitExceeded'
  AND Timestamp >= DATEADD(hour, -1, GETUTCDATE())
GROUP BY UserId
ORDER BY Violations DESC;
```

## Troubleshooting

### Middleware nie działa

Sprawdź kolejność middleware:
```csharp
app.UseSecurityAudit();    // Przed authentication!
app.UseRateLimiting();     // Przed authentication!
app.UseAuthentication();
app.UseAuthorization();
```

### Rate limits zbyt restrykcyjne

Dostosuj limity w `RateLimitingService._rateLimits`:
```csharp
["send_message"] = (TimeSpan.FromMinutes(1), 100),  // Zwiększ z 60 do 100
```

### Audit logs nie pojawiają się

1. Sprawdź czy SecurityModule jest zarejestrowany
2. Sprawdź connection string do SecurityDatabase
3. Sprawdź czy migracje zostały uruchomione
4. Sprawdź logi Serilog

## Przyszłe Ulepszenia

### Planowane Funkcje:
- ⏳ Redis-based distributed rate limiting
- ⏳ Email/Webhook alerting dla Critical events
- ⏳ Admin dashboard do przeglądania audit logs
- ⏳ Machine learning anomaly detection
- ⏳ Geographic IP blocking
- ⏳ 2FA/MFA integration
- ⏳ Session management z device tracking

## License

Part of Aegis Messenger - see main repository LICENSE.
