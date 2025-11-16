# 🚀 Implementation Guide - Modular Architecture

> Przewodnik po implementacji wielomodułowej architektury Aegis Messenger

---

## 📊 Stan Implementacji

### ✅ Ukończone

- [x] Dokumentacja architektury modularnej (MODULAR_ARCHITECTURE.md)
- [x] Struktura katalogów (`src-v2/`)
- [x] Nowa solution file (`Aegis.Modular.sln`)
- [x] Aegis.Shared.Kernel - podstawowe klasy

### 🚧 W Trakcie

- [ ] Shared libraries (Contracts, Cryptography, Infrastructure)
- [ ] Moduły domenowe (Auth, Messages, Users, Groups, Files)
- [ ] Host API
- [ ] Klienty (Desktop, Android)

---

## 🎯 Plan Migracji Krok po Kroku

### Faza 1: Shared Libraries (Tydzień 1)

#### 1.1 Aegis.Shared.Kernel ✅

**Status:** Rozpoczęty

**Pozostałe pliki do utworzenia:**

```bash
src-v2/Shared/Aegis.Shared.Kernel/
├── Primitives/
│   ├── Entity.cs ✅
│   ├── AggregateRoot.cs
│   ├── ValueObject.cs
│   └── DomainEvent.cs
├── ValueObjects/
│   ├── Email.cs
│   ├── PhoneNumber.cs
│   └── Username.cs
├── Interfaces/
│   ├── IEntity.cs
│   ├── IAggregateRoot.cs
│   ├── IDomainEvent.cs
│   └── IEventHandler.cs
└── Results/
    ├── Result.cs
    ├── Result{T}.cs
    └── Error.cs
```

**Przykładowa implementacja ValueObject:**

```csharp
// ValueObjects/Email.cs
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(new Error(
                "Email.Empty",
                "Email cannot be empty"));

        if (!IsValidEmail(email))
            return Result.Failure<Email>(new Error(
                "Email.InvalidFormat",
                "Email format is invalid"));

        return Result.Success(new Email(email));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    private static bool IsValidEmail(string email)
        => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
```

#### 1.2 Aegis.Shared.Contracts

**Pliki do utworzenia:**

```csharp
// DTOs/Auth/LoginRequest.cs
public record LoginRequest(
    string Username,
    string Password
);

public record LoginResponse(
    Guid UserId,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);

// DTOs/Messages/SendMessageRequest.cs
public record SendMessageRequest(
    Guid RecipientId,
    byte[] EncryptedContent,
    bool IsGroup = false,
    Guid? GroupId = null
);

// Events/UserRegisteredEvent.cs
public record UserRegisteredEvent(
    Guid UserId,
    string Username,
    DateTime OccurredAt
) : IDomainEvent;
```

#### 1.3 Aegis.Shared.Cryptography

**Migracja z src/Aegis.Core/Cryptography:**

```bash
# Kopiuj istniejący kod
cp -r src/Aegis.Core/Cryptography/* src-v2/Shared/Aegis.Shared.Cryptography/

# Zmień namespace
# Z: namespace Aegis.Core.Cryptography
# Na: namespace Aegis.Shared.Cryptography
```

**Struktura:**

```
Aegis.Shared.Cryptography/
├── SignalProtocol/
│   ├── ISignalProtocol.cs
│   ├── SignalSessionManager.cs (z Aegis.Core)
│   └── SignalKeyManager.cs
├── Encryption/
│   ├── IAesEncryption.cs
│   ├── AesGcmEncryptionService.cs (z Aegis.Core)
│   └── KeyDerivationService.cs (z Aegis.Core)
└── Storage/
    ├── IKeyStore.cs
    ├── WindowsKeyStore.cs (DPAPI)
    └── AndroidKeyStore.cs
```

#### 1.4 Aegis.Shared.Infrastructure

**Kluczowe komponenty:**

```csharp
// Persistence/BaseRepository.cs
public abstract class BaseRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public virtual async Task<TEntity?> GetByIdAsync(TId id)
        => await _dbSet.FindAsync(id);

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }
}

// EventBus/InMemoryEventBus.cs
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;

    public async Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event);
        }
    }
}

// Caching/ICacheService.cs
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}
```

---

### Faza 2: Module Auth (Tydzień 2)

#### 2.1 Domain Layer

```csharp
// Domain/Entities/User.cs
public class User : AggregateRoot<Guid>
{
    public Username Username { get; private set; }
    public Email Email { get; private set; }
    public HashedPassword Password { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core

    public static Result<User> Create(
        Username username,
        Email email,
        string plainPassword,
        IPasswordHasher passwordHasher)
    {
        var hashedPassword = passwordHasher.HashPassword(plainPassword);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            Password = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(
            user.Id,
            username.Value,
            DateTime.UtcNow));

        return Result.Success(user);
    }

    public Result<bool> VerifyPassword(
        string passwordToVerify,
        IPasswordHasher passwordHasher)
    {
        var isValid = passwordHasher.VerifyPassword(
            passwordToVerify,
            Password.Value);

        if (!isValid)
            return Result.Failure<bool>(new Error(
                "User.InvalidPassword",
                "Password is incorrect"));

        return Result.Success(true);
    }
}

// Domain/ValueObjects/Username.cs
public sealed class Username : ValueObject
{
    public string Value { get; }

    private Username(string value) => Value = value;

    public static Result<Username> Create(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result.Failure<Username>(new Error(
                "Username.Empty",
                "Username cannot be empty"));

        if (username.Length < 3 || username.Length > 50)
            return Result.Failure<Username>(new Error(
                "Username.InvalidLength",
                "Username must be between 3 and 50 characters"));

        if (!Regex.IsMatch(username, "^[a-zA-Z0-9_-]+$"))
            return Result.Failure<Username>(new Error(
                "Username.InvalidFormat",
                "Username can only contain alphanumeric, underscore, and dash"));

        return Result.Success(new Username(username));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}
```

#### 2.2 Application Layer

```csharp
// Application/Commands/Register/RegisterCommand.cs
public record RegisterCommand(
    string Username,
    string Email,
    string Password
) : ICommand<Result<RegisterResult>>;

public record RegisterResult(Guid UserId);

// Application/Commands/Register/RegisterCommandHandler.cs
public class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, Result<RegisterResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public async Task<Result<RegisterResult>> Handle(
        RegisterCommand command,
        CancellationToken ct)
    {
        // 1. Create value objects
        var usernameResult = Username.Create(command.Username);
        if (usernameResult.IsFailure)
            return Result.Failure<RegisterResult>(usernameResult.Error);

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result.Failure<RegisterResult>(emailResult.Error);

        // 2. Check uniqueness
        if (await _userRepository.ExistsByUsernameAsync(usernameResult.Value))
            return Result.Failure<RegisterResult>(new Error(
                "User.DuplicateUsername",
                "Username already exists"));

        // 3. Create user
        var userResult = User.Create(
            usernameResult.Value,
            emailResult.Value,
            command.Password,
            _passwordHasher);

        if (userResult.IsFailure)
            return Result.Failure<RegisterResult>(userResult.Error);

        // 4. Save
        var user = userResult.Value;
        await _userRepository.AddAsync(user);
        await _unitOfWork.CommitAsync(ct);

        // 5. Publish events
        foreach (var domainEvent in user.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent);
        }

        user.ClearDomainEvents();

        return Result.Success(new RegisterResult(user.Id));
    }
}
```

#### 2.3 Infrastructure Layer

```csharp
// Infrastructure/Persistence/AuthDbContext.cs
public class AuthDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());
    }
}

// Infrastructure/Persistence/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .HasConversion(
                u => u.Value,
                v => Username.Create(v).Value)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .HasConversion(
                e => e.Value,
                v => Email.Create(v).Value)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Ignore(u => u.DomainEvents);
    }
}

// Infrastructure/Repositories/UserRepository.cs
public class UserRepository : BaseRepository<User, Guid>, IUserRepository
{
    public UserRepository(AuthDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsByUsernameAsync(Username username)
    {
        return await _dbSet.AnyAsync(u => u.Username == username);
    }

    public async Task<User?> GetByUsernameAsync(Username username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }
}
```

#### 2.4 API Layer

```csharp
// API/Controllers/AuthController.cs
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password);

        var result = await _sender.Send(command);

        return result.IsSuccess
            ? Ok(new { userId = result.Value.UserId })
            : BadRequest(new { error = result.Error.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password);

        var result = await _sender.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(new { error = result.Error.Message });
    }
}

// API/ModuleRegistration.cs
public static class AuthModuleRegistration
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AuthDatabase")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Services
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(RegisterCommandHandler).Assembly));

        return services;
    }
}
```

---

### Faza 3: Module Messages (Tydzień 3)

Podobna struktura jak Auth module:

```
Messages/
├── Domain/
│   ├── Entities/
│   │   ├── Message.cs
│   │   └── EncryptionSession.cs
│   ├── ValueObjects/
│   │   └── EncryptedContent.cs
│   └── Events/
│       ├── MessageSentEvent.cs
│       └── MessageDeliveredEvent.cs
├── Application/
│   ├── Commands/
│   │   ├── SendMessage/
│   │   └── MarkAsRead/
│   └── Queries/
│       └── GetConversation/
├── Infrastructure/
│   ├── Persistence/
│   │   └── MessagesDbContext.cs
│   └── SignalProtocol/
│       └── SignalProtocolService.cs
└── API/
    ├── Controllers/
    │   └── MessagesController.cs
    └── Hubs/
        └── MessageHub.cs
```

---

### Faza 4: Host API (Tydzień 4)

```csharp
// Host/Aegis.Host.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add modules
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddMessagesModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddGroupsModule(builder.Configuration);
builder.Services.AddFilesModule(builder.Configuration);

// Shared services
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddEventBus();

// Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapHub<MessageHub>("/hubs/messages");

app.Run();
```

---

## 🧪 Testing

### Architecture Tests

```csharp
public class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(User).Assembly)
            .That().ResideInNamespace("Domain")
            .ShouldNot().HaveDependencyOn("Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_API()
    {
        var result = Types.InAssembly(typeof(RegisterCommand).Assembly)
            .ShouldNot().HaveDependencyOn("API")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
```

---

## 📚 Następne Kroki

1. **Ukończ Shared Libraries** (Tydzień 1)
2. **Implementuj Auth Module** (Tydzień 2)
3. **Implementuj Messages Module** (Tydzień 3)
4. **Pozostałe Modules** (Tydzień 4-5)
5. **Migruj Clients** (Tydzień 6)
6. **Testing & Documentation** (Tydzień 7)

---

**Total Time:** 7 tygodni (1.5 miesiąca)
