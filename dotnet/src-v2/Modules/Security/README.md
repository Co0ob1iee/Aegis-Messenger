# Aegis Security Module

Kompletny moduł bezpieczeństwa dla Aegis Messenger, zapewniający:
- **Security Audit Logging** - kompletny audit trail wszystkich zdarzeń
- **Rate Limiting** - ochrona przed abuse i brute force (in-memory + Redis distributed)
- **Email/Webhook Alerting** - automatyczne powiadomienia dla Critical events
- **Platform-Specific Key Storage** - bezpieczne przechowywanie kluczy
  - Windows: DPAPI (Data Protection API)
  - Android: Android KeyStore System (hardware-backed)
  - Linux: będzie dodany (KeyRing/libsecret)
  - Fallback: In-Memory (development only)
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

### 6. Admin Dashboard API

Kompletne REST API do przeglądania audit logs przez administratorów.

**Authorization:** Wszystkie endpointy wymagają roli `Admin`

**Dostępne endpointy:**

#### GET `/api/admin/security/audit-logs`
Pobiera stronicowane audit logs z zaawansowanym filtrowaniem.

**Query Parameters:**
- `pageNumber` (int, default: 1) - numer strony
- `pageSize` (int, default: 50, max: 200) - rozmiar strony
- `userId` (guid?) - filtruj po user ID
- `ipAddress` (string?) - filtruj po IP address
- `eventType` (SecurityEventType?) - filtruj po typie eventu
- `severity` (SecurityEventSeverity?) - filtruj po severity
- `isSuccessful` (bool?) - filtruj po statusie (success/failure)
- `from` (datetime?) - od kiedy
- `to` (datetime?) - do kiedy
- `sortBy` (string, default: "Timestamp") - kolumna sortowania
- `sortDescending` (bool, default: true) - kierunek sortowania

**Response:** `PagedResult<SecurityAuditLogDto>`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "eventType": "LoginFailed",
      "severity": "High",
      "timestamp": "2025-11-16T14:30:00Z",
      "isSuccessful": false,
      "userId": "abc123...",
      "username": "john.doe",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "errorMessage": "Invalid credentials",
      "details": "Login attempt from suspicious location"
    }
  ],
  "totalCount": 1250,
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 25,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Przykład użycia:**
```bash
# Wszystkie failed login attempts z ostatnich 24h
curl -H "Authorization: Bearer {token}" \
  "https://api.aegis.com/api/admin/security/audit-logs?eventType=LoginFailed&from=2025-11-15T14:00:00Z&pageSize=100"

# Critical events dla konkretnego użytkownika
curl -H "Authorization: Bearer {token}" \
  "https://api.aegis.com/api/admin/security/audit-logs?userId=abc123&severity=Critical"

# Wszystkie failures z konkretnego IP
curl -H "Authorization: Bearer {token}" \
  "https://api.aegis.com/api/admin/security/audit-logs?ipAddress=192.168.1.100&isSuccessful=false"
```

#### GET `/api/admin/security/audit-logs/{id}`
Pobiera pojedynczy audit log po ID.

**Response:** `SecurityAuditLogDto` lub 404

#### GET `/api/admin/security/audit-logs/user/{userId}`
Pobiera audit logs dla konkretnego użytkownika.

**Query Parameters:**
- `pageNumber`, `pageSize`, `from`, `to`

**Response:** `PagedResult<SecurityAuditLogDto>`

#### GET `/api/admin/security/statistics`
Pobiera statystyki audit logs dla dashboardu.

**Query Parameters:**
- `from` (datetime?) - od kiedy
- `to` (datetime?) - do kiedy
- `topUsersLimit` (int, default: 10, max: 50) - liczba top użytkowników
- `topIpsLimit` (int, default: 10, max: 50) - liczba top IP addresses

**Response:** `AuditLogStatisticsDto`
```json
{
  "totalEvents": 15430,
  "totalFailures": 245,
  "criticalEvents": 12,
  "highSeverityEvents": 58,
  "eventTypeCounts": {
    "LoginSuccess": 8500,
    "LoginFailed": 145,
    "MessageSent": 5200,
    "PasswordChanged": 23
  },
  "severityCounts": {
    "Info": 13500,
    "Low": 1200,
    "Medium": 660,
    "High": 58,
    "Critical": 12
  },
  "topActiveUsers": [
    {
      "userId": "abc123...",
      "username": "john.doe",
      "eventCount": 1250,
      "failureCount": 5
    }
  ],
  "topIpAddresses": [
    {
      "ipAddress": "192.168.1.100",
      "eventCount": 850,
      "failureCount": 2
    }
  ],
  "oldestEvent": "2025-01-01T00:00:00Z",
  "newestEvent": "2025-11-16T14:30:00Z"
}
```

**Przykład dashboardu:**
```typescript
// React/TypeScript example
const DashboardStats = () => {
  const { data: stats } = useQuery('security-stats', () =>
    fetch('/api/admin/security/statistics?from=2025-11-01').then(r => r.json())
  );

  return (
    <div>
      <StatCard title="Total Events" value={stats.totalEvents} />
      <StatCard title="Failures" value={stats.totalFailures} color="red" />
      <StatCard title="Critical" value={stats.criticalEvents} color="red" />

      <Chart data={stats.eventTypeCounts} />
      <TopUsersTable users={stats.topActiveUsers} />
      <TopIpsTable ips={stats.topIpAddresses} />
    </div>
  );
};
```

#### GET `/api/admin/security/audit-logs/export`
Eksportuje audit logs do CSV.

**Query Parameters:** te same co `/audit-logs` (bez paginacji)

**Response:** Plik CSV (max 10,000 rekordów)

**Przykład użycia:**
```bash
# Export wszystkich failed logins z ostatniego miesiąca
curl -H "Authorization: Bearer {token}" \
  "https://api.aegis.com/api/admin/security/audit-logs/export?isSuccessful=false&from=2025-10-16" \
  -o audit-logs.csv
```

**Format CSV:**
```csv
Id,Timestamp,EventType,Severity,IsSuccessful,UserId,Username,IpAddress,...
"abc123...","2025-11-16 14:30:00","LoginFailed","High","False","user123","john.doe","192.168.1.100",...
```

**CQRS Architecture:**
API używa MediatR i CQRS pattern:
- `GetAuditLogsQuery` - paginated list z filtrowaniem
- `GetAuditLogByIdQuery` - single log
- `GetUserAuditLogsQuery` - user-specific logs
- `GetAuditLogStatisticsQuery` - dashboard statistics

**Query Handlers:**
- `GetAuditLogsQueryHandler` - zaawansowane filtrowanie i sortowanie
- `GetAuditLogByIdQueryHandler` - single record retrieval
- `GetUserAuditLogsQueryHandler` - user activity history
- `GetAuditLogStatisticsQueryHandler` - aggregated statistics

**Performance:**
- Pagination: max 200 items per page
- Export: max 10,000 records
- Statistics: agregacja w pamięci (używaj `from`/`to` dla dużych datasetów)
- Indeksy: Timestamp, UserId, EventType, Severity (już skonfigurowane)

### 7. Helper Extensions

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

### 8. Queries - Przeglądanie Audit Logs (Programmatic)

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

## Platform-Specific Key Storage

### Overview

Aegis Messenger używa platform-specific secure storage dla kluczy kryptograficznych:
- **Windows**: DPAPI (Data Protection API)
- **Android**: Android KeyStore System (hardware-backed when available)
- **Linux**: (w przygotowaniu) Linux KeyRing / libsecret
- **Fallback**: In-Memory (tylko development)

### Windows DPAPI

**Lokalizacja**: `Shared/Aegis.Shared.Cryptography/Storage/WindowsDpapiKeyStore.cs`

**Features:**
- Encryption at rest używając Windows Data Protection API
- Protected with user credentials (CurrentUser scope)
- Keys can only be decrypted by the same Windows user
- Secure deletion z 3-pass overwrite (DoD 5220.22-M)
- User isolation - każdy user ma własny katalog

**Storage Path**: `%LOCALAPPDATA%\AegisMessenger\Keys\{userId}\`

**Bezpieczeństwo:**
- ✅ DPAPI używa user master key z Windows Credential Manager
- ✅ Additional entropy layer
- ✅ Filesystem permissions (tylko current user)
- ⚠️ Software-based (nie hardware-backed)
- ⚠️ Vulnerabilities: admin access, memory dumps

**Przykład użycia:**
```csharp
var keyStore = new WindowsDpapiKeyStore(logger);

// Store key
await keyStore.StoreKeyAsync("signal-identity", identityKey, userId);

// Retrieve key
var key = await keyStore.RetrieveKeyAsync("signal-identity", userId);

// Delete key (secure 3-pass wipe)
await keyStore.DeleteKeyAsync("signal-identity", userId);

// Delete all keys for user
await keyStore.DeleteAllKeysAsync(userId);
```

### Android KeyStore

**Lokalizacja**: `Shared/Aegis.Shared.Cryptography/Storage/AndroidKeyStore.cs`

**Features:**
- **Hardware-backed** when available (TEE/StrongBox)
- Master keys stored in Android KeyStore System
- Biometric authentication support (production implementation)
- Keys never leave secure hardware
- User isolation - każdy user ma własny katalog

**Storage Path**: `{AppDataDirectory}/keys/{userId}/`

**Bezpieczeństwo:**
- ✅ Hardware-backed on most devices (ARM TrustZone)
- ✅ **StrongBox** support on Android 9+ flagship devices
- ✅ Biometric authentication (production)
- ✅ Key attestation - verify hardware backing
- ✅ Keys invalidated on factory reset
- ⚠️ Current implementation: simplified demo
- ⚠️ Production: use AndroidX.Security.Crypto

**Current Implementation (Demo):**
```csharp
// SIMPLIFIED VERSION - for cross-platform development
var keyStore = new AndroidKeyStore(logger);
await keyStore.StoreKeyAsync("signal-identity", identityKey, userId);
```

**Production Implementation:**

See `ANDROID_KEYSTORE_PRODUCTION.md` for complete guide.

Recommended approach with AndroidX Security:
```csharp
using AndroidX.Security.Crypto;

public class ProductionAndroidKeyStore : IKeyStore
{
    private readonly MasterKey _masterKey;

    public ProductionAndroidKeyStore(Context context)
    {
        // Create master key in Android KeyStore
        _masterKey = new MasterKey.Builder(context)
            .SetKeyScheme(MasterKey.KeyScheme.Aes256Gcm)
            .SetUserAuthenticationRequired(true, 30)  // Biometric auth, 30s validity
            .Build();
    }

    public async Task StoreKeyAsync(string keyId, byte[] key, Guid userId)
    {
        var file = new Java.IO.File(context.FilesDir, GetFileName(keyId, userId));

        var encryptedFile = new EncryptedFile.Builder(
            context,
            file,
            _masterKey,
            EncryptedFile.FileEncryptionScheme.Aes256GcmHkdfTStream
        ).Build();

        using var output = encryptedFile.OpenFileOutput();
        await output.WriteAsync(key);
    }
}
```

**Hardware Features:**
- **TEE (Trusted Execution Environment)**: ARM TrustZone - isolated CPU mode
- **StrongBox**: Dedicated security chip (Pixel 3+, Samsung S9+)
- **Biometric Protection**: Fingerprint, Face, Iris unlock
- **Key Attestation**: Cryptographic proof of hardware backing

**Check hardware support:**
```csharp
var keyInfo = GetKeyInfo(alias);
bool isHardwareBacked = keyInfo.IsInsideSecureHardware;
bool isStrongBox = keyInfo.SecurityLevel == SecurityLevel.StrongBox;
```

### In-Memory KeyStore (Development Only)

**Lokalizacja**: `Shared/Aegis.Shared.Cryptography/Storage/InMemoryKeyStore.cs`

**⚠️ WARNING**: Only for development/testing!

**Features:**
- Thread-safe ConcurrentDictionary
- User isolation z key prefix: `{userId}:{keyId}`
- ❌ Keys lost on app restart
- ❌ No encryption at rest
- ❌ Vulnerable to memory dumps

**Przykład użycia:**
```csharp
// ONLY for development
services.AddSingleton<IKeyStore, InMemoryKeyStore>();
```

### Linux KeyRing (Coming Soon - ZADANIE 5)

Będzie używał:
- **libsecret** (GNOME Keyring)
- **KWallet** (KDE)
- **Secret Service API** (freedesktop.org standard)

### Wybór Implementacji

**Automatic platform detection:**
```csharp
services.AddSingleton<IKeyStore>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<IKeyStore>>();

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return new WindowsDpapiKeyStore(logger);
    }
    else if (OperatingSystem.IsAndroid())
    {
        // Production: use ProductionAndroidKeyStore
        return new AndroidKeyStore(logger);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        // TODO: return new LinuxKeyRingStore(logger);
        throw new PlatformNotSupportedException("Linux KeyRing not yet implemented");
    }
    else
    {
        // Fallback for development
        logger.LogWarning("Using InMemoryKeyStore - NOT SECURE FOR PRODUCTION");
        return new InMemoryKeyStore();
    }
});
```

### Security Comparison

| Feature | Windows DPAPI | Android KeyStore | Linux KeyRing | In-Memory |
|---------|--------------|------------------|---------------|-----------|
| Encryption at rest | ✅ | ✅ | ✅ | ❌ |
| Hardware-backed | ❌ | ✅ (most devices) | ⚠️ (depends) | ❌ |
| Biometric auth | ❌ | ✅ | ⚠️ (depends) | ❌ |
| Survives restart | ✅ | ✅ | ✅ | ❌ |
| User isolation | ✅ | ✅ | ✅ | ✅ |
| Secure deletion | ✅ | ✅ | ⚠️ | N/A |
| Production ready | ✅ | ⚠️ (use AndroidX) | ❌ (coming soon) | ❌ |

### Best Practices

1. **Always use platform-specific storage in production**
2. **Enable biometric authentication on Android** for sensitive keys
3. **Test on multiple devices** - hardware support varies
4. **Implement key recovery mechanism** - users can lose biometric access
5. **Rotate keys periodically** - even hardware-backed keys should be rotated
6. **Monitor key access** - log to security audit when keys are accessed
7. **Handle key loss gracefully** - app reinstall, device migration

### Migration

When changing KeyStore implementation:
```csharp
public class MigratingKeyStore : IKeyStore
{
    public async Task<byte[]?> RetrieveKeyAsync(string keyId, Guid userId)
    {
        // Try new storage first
        var key = await _newKeyStore.RetrieveKeyAsync(keyId, userId);
        if (key != null) return key;

        // Fallback to old storage
        key = await _oldKeyStore.RetrieveKeyAsync(keyId, userId);
        if (key != null)
        {
            // Migrate to new storage
            await _newKeyStore.StoreKeyAsync(keyId, key, userId);
            await _oldKeyStore.DeleteKeyAsync(keyId, userId);
            _logger.LogInformation("Migrated key {KeyId} to new storage", keyId);
        }

        return key;
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
