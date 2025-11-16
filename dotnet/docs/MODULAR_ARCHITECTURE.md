# 🏗️ Aegis Messenger - Modular Architecture Design

> **Enterprise-Grade Multi-Module Architecture**
>
> **Version:** 2.0
> **Date:** 2025-11-16
> **Architecture Style:** Modular Monolith → Microservices Ready

---

## 📋 Table of Contents

1. [Architecture Vision](#architecture-vision)
2. [Design Principles](#design-principles)
3. [Module Structure](#module-structure)
4. [Shared Libraries](#shared-libraries)
5. [Module Details](#module-details)
6. [Data Architecture](#data-architecture)
7. [Communication Patterns](#communication-patterns)
8. [Migration Plan](#migration-plan)

---

## 🎯 Architecture Vision

### Current State (v1.0)

```
❌ PROBLEMS:
- Monolithic structure
- Tight coupling between components
- Single database for all domains
- No clear boundaries
- Difficult to scale independently
- Shared responsibilities
```

### Target State (v2.0)

```
✅ GOALS:
- Modular Monolith architecture
- Clear domain boundaries
- Independent deployment capability (microservices ready)
- Loose coupling, high cohesion
- Dedicated infrastructure per module
- Event-driven communication between modules
```

---

## 📐 Design Principles

### 1. **Domain-Driven Design (DDD)**
- Each module represents a Bounded Context
- Ubiquitous language within each module
- Domain events for inter-module communication

### 2. **Clean Architecture**
```
┌─────────────────────────────────────┐
│         Presentation Layer          │  ← Controllers, Views
├─────────────────────────────────────┤
│         Application Layer           │  ← Use Cases, DTOs
├─────────────────────────────────────┤
│           Domain Layer              │  ← Entities, Domain Logic
├─────────────────────────────────────┤
│       Infrastructure Layer          │  ← EF Core, External APIs
└─────────────────────────────────────┘
```

### 3. **SOLID Principles**
- **S**ingle Responsibility
- **O**pen/Closed
- **L**iskov Substitution
- **I**nterface Segregation
- **D**ependency Inversion

### 4. **Microservices Readiness**
- Each module can be extracted to microservice
- Own database per module
- API Gateway ready
- Event bus integration

---

## 🏛️ Module Structure

### High-Level Architecture

```
Aegis-Messenger/
│
├── src/
│   ├── Shared/                          # 📚 Shared Libraries
│   │   ├── Aegis.Shared.Contracts/
│   │   ├── Aegis.Shared.Cryptography/
│   │   ├── Aegis.Shared.Infrastructure/
│   │   └── Aegis.Shared.Kernel/
│   │
│   ├── Modules/                         # 🎯 Domain Modules
│   │   ├── Auth/
│   │   │   ├── Aegis.Modules.Auth.Domain/
│   │   │   ├── Aegis.Modules.Auth.Application/
│   │   │   ├── Aegis.Modules.Auth.Infrastructure/
│   │   │   └── Aegis.Modules.Auth.API/
│   │   │
│   │   ├── Messages/
│   │   │   ├── Aegis.Modules.Messages.Domain/
│   │   │   ├── Aegis.Modules.Messages.Application/
│   │   │   ├── Aegis.Modules.Messages.Infrastructure/
│   │   │   └── Aegis.Modules.Messages.API/
│   │   │
│   │   ├── Users/
│   │   ├── Groups/
│   │   ├── Files/
│   │   └── Notifications/
│   │
│   ├── Clients/                         # 💻 Client Applications
│   │   ├── Aegis.Clients.Desktop/       # WinUI 3
│   │   ├── Aegis.Clients.Android/       # .NET MAUI
│   │   └── Aegis.Clients.Shared/        # Shared UI code
│   │
│   └── Host/                            # 🌐 API Host
│       └── Aegis.Host.API/              # Modular Monolith Host
│
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   └── ArchitectureTests/               # Enforce architecture rules
│
└── docs/
    ├── architecture/
    └── modules/
```

---

## 📚 Shared Libraries

### Aegis.Shared.Contracts

**Purpose:** DTOs, Interfaces, Common Contracts

```
Aegis.Shared.Contracts/
├── DTOs/
│   ├── Messages/
│   │   ├── MessageDto.cs
│   │   ├── SendMessageRequest.cs
│   │   └── SendMessageResponse.cs
│   ├── Users/
│   │   ├── UserDto.cs
│   │   └── UserProfileDto.cs
│   └── Auth/
│       ├── LoginRequest.cs
│       └── LoginResponse.cs
│
├── Interfaces/
│   ├── IEventBus.cs
│   ├── ICurrentUserService.cs
│   └── IDateTimeProvider.cs
│
└── Events/                              # Domain Events
    ├── MessageSentEvent.cs
    ├── UserRegisteredEvent.cs
    └── GroupCreatedEvent.cs
```

**Key Classes:**

```csharp
// DTOs/Messages/SendMessageRequest.cs
namespace Aegis.Shared.Contracts.DTOs.Messages;

public record SendMessageRequest(
    Guid RecipientId,
    string Content,
    bool IsGroup = false,
    Guid? GroupId = null,
    Guid? FileAttachmentId = null
);

public record SendMessageResponse(
    Guid MessageId,
    DateTime Timestamp,
    MessageStatus Status
);

// Events/MessageSentEvent.cs
namespace Aegis.Shared.Contracts.Events;

public record MessageSentEvent(
    Guid MessageId,
    Guid SenderId,
    Guid RecipientId,
    DateTime Timestamp
) : IDomainEvent;
```

---

### Aegis.Shared.Cryptography

**Purpose:** Signal Protocol, AES Encryption, Key Management

```
Aegis.Shared.Cryptography/
├── SignalProtocol/
│   ├── ISignalProtocol.cs
│   ├── SignalSessionManager.cs
│   ├── SignalKeyManager.cs
│   └── PreKeyBundleGenerator.cs
│
├── Encryption/
│   ├── IAesEncryption.cs
│   ├── AesGcmEncryptionService.cs
│   └── KeyDerivationService.cs
│
├── Hashing/
│   ├── IPasswordHasher.cs
│   └── Argon2PasswordHasher.cs
│
└── Storage/
    ├── IKeyStore.cs
    ├── WindowsKeyStore.cs         # DPAPI
    └── AndroidKeyStore.cs         # Android Keystore
```

**Key Interfaces:**

```csharp
// SignalProtocol/ISignalProtocol.cs
namespace Aegis.Shared.Cryptography.SignalProtocol;

public interface ISignalProtocol
{
    Task<PreKeyBundle> GeneratePreKeyBundleAsync(
        Guid userId,
        IdentityKeyPair identityKeyPair);

    Task<bool> InitializeSessionAsync(
        Guid recipientId,
        PreKeyBundle preKeyBundle);

    Task<byte[]> EncryptMessageAsync(
        Guid recipientId,
        string plaintext);

    Task<string> DecryptMessageAsync(
        Guid senderId,
        byte[] ciphertext);
}

// Encryption/IAesEncryption.cs
public interface IAesEncryption
{
    Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key);
    Task<byte[]> DecryptAsync(byte[] ciphertext, byte[] key);
    byte[] GenerateKey();
}
```

---

### Aegis.Shared.Infrastructure

**Purpose:** Common Infrastructure Components

```
Aegis.Shared.Infrastructure/
├── Persistence/
│   ├── BaseRepository.cs
│   ├── UnitOfWork.cs
│   └── AuditableEntity.cs
│
├── EventBus/
│   ├── InMemoryEventBus.cs
│   ├── RabbitMqEventBus.cs          # For microservices
│   └── EventBusExtensions.cs
│
├── Caching/
│   ├── ICacheService.cs
│   ├── MemoryCacheService.cs
│   └── RedisCacheService.cs
│
├── Logging/
│   └── SerilogConfiguration.cs
│
└── Exceptions/
    ├── NotFoundException.cs
    ├── ValidationException.cs
    └── UnauthorizedException.cs
```

**Key Classes:**

```csharp
// Persistence/BaseRepository.cs
namespace Aegis.Shared.Infrastructure.Persistence;

public abstract class BaseRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected BaseRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id)
        => await _dbSet.FindAsync(id);

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    // ... other methods
}

// EventBus/InMemoryEventBus.cs
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;

    public async Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider
            .GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event);
        }
    }
}
```

---

### Aegis.Shared.Kernel

**Purpose:** Domain Primitives, Value Objects, Base Classes

```
Aegis.Shared.Kernel/
├── Primitives/
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   ├── ValueObject.cs
│   └── DomainEvent.cs
│
├── ValueObjects/
│   ├── Email.cs
│   ├── PhoneNumber.cs
│   ├── Username.cs
│   └── EncryptedData.cs
│
├── Interfaces/
│   ├── IEntity.cs
│   ├── IAggregateRoot.cs
│   └── IDomainEvent.cs
│
└── Results/
    ├── Result.cs
    └── ResultExtensions.cs
```

**Key Classes:**

```csharp
// Primitives/Entity.cs
namespace Aegis.Shared.Kernel.Primitives;

public abstract class Entity<TId> : IEntity<TId>
{
    public TId Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// ValueObjects/Email.cs
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>("Email cannot be empty");

        if (!IsValidEmail(email))
            return Result.Failure<Email>("Invalid email format");

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

---

## 🎯 Module Details

### Module: Auth

**Responsibilities:**
- User authentication
- Token generation (JWT)
- Password management
- Two-factor authentication

**Structure:**

```
Modules/Auth/
├── Aegis.Modules.Auth.Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   └── RefreshToken.cs
│   ├── ValueObjects/
│   │   ├── Password.cs
│   │   └── TwoFactorCode.cs
│   ├── Events/
│   │   ├── UserRegisteredEvent.cs
│   │   └── UserLoggedInEvent.cs
│   └── Interfaces/
│       └── IUserRepository.cs
│
├── Aegis.Modules.Auth.Application/
│   ├── Commands/
│   │   ├── Register/
│   │   │   ├── RegisterCommand.cs
│   │   │   └── RegisterCommandHandler.cs
│   │   └── Login/
│   │       ├── LoginCommand.cs
│   │       └── LoginCommandHandler.cs
│   ├── Queries/
│   │   └── GetCurrentUser/
│   │       ├── GetCurrentUserQuery.cs
│   │       └── GetCurrentUserQueryHandler.cs
│   └── Services/
│       ├── IJwtTokenGenerator.cs
│       └── IPasswordHasher.cs
│
├── Aegis.Modules.Auth.Infrastructure/
│   ├── Persistence/
│   │   ├── AuthDbContext.cs
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs
│   │   └── Migrations/
│   └── Services/
│       ├── JwtTokenGenerator.cs
│       └── Argon2PasswordHasher.cs
│
└── Aegis.Modules.Auth.API/
    ├── Controllers/
    │   └── AuthController.cs
    ├── Endpoints/                  # Minimal APIs alternative
    │   ├── RegisterEndpoint.cs
    │   └── LoginEndpoint.cs
    └── ModuleRegistration.cs       # DI registration
```

**Example Code:**

```csharp
// Domain/Entities/User.cs
namespace Aegis.Modules.Auth.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    public Username Username { get; private set; }
    public Email Email { get; private set; }
    public Password Password { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core

    public static Result<User> Create(
        Username username,
        Email email,
        Password password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            Password = password,
            CreatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, username.Value));
        return Result.Success(user);
    }

    public Result VerifyPassword(string passwordToVerify)
    {
        return Password.Verify(passwordToVerify)
            ? Result.Success()
            : Result.Failure("Invalid password");
    }
}

// Application/Commands/Register/RegisterCommandHandler.cs
public class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public async Task<Result<RegisterResult>> Handle(
        RegisterCommand command,
        CancellationToken ct)
    {
        // 1. Create value objects
        var usernameResult = Username.Create(command.Username);
        var emailResult = Email.Create(command.Email);
        var passwordResult = Password.Create(command.Password);

        if (usernameResult.IsFailure || emailResult.IsFailure || passwordResult.IsFailure)
            return Result.Failure<RegisterResult>("Invalid input");

        // 2. Check if user exists
        if (await _userRepository.ExistsByUsernameAsync(usernameResult.Value))
            return Result.Failure<RegisterResult>("Username already exists");

        // 3. Create user
        var userResult = User.Create(
            usernameResult.Value,
            emailResult.Value,
            passwordResult.Value);

        if (userResult.IsFailure)
            return Result.Failure<RegisterResult>(userResult.Error);

        // 4. Save
        await _userRepository.AddAsync(userResult.Value);
        await _unitOfWork.CommitAsync(ct);

        // 5. Publish events
        foreach (var domainEvent in userResult.Value.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent);
        }

        return Result.Success(new RegisterResult(userResult.Value.Id));
    }
}
```

---

### Module: Messages

**Responsibilities:**
- Send/receive encrypted messages
- Message storage
- Signal Protocol session management
- Message status tracking

**Structure:**

```
Modules/Messages/
├── Aegis.Modules.Messages.Domain/
│   ├── Entities/
│   │   ├── Message.cs
│   │   └── EncryptionSession.cs
│   ├── ValueObjects/
│   │   ├── EncryptedContent.cs
│   │   └── MessageId.cs
│   ├── Events/
│   │   ├── MessageSentEvent.cs
│   │   └── MessageDeliveredEvent.cs
│   └── Services/
│       └── ISignalProtocolService.cs
│
├── Aegis.Modules.Messages.Application/
│   ├── Commands/
│   │   ├── SendMessage/
│   │   ├── DeleteMessage/
│   │   └── MarkAsRead/
│   └── Queries/
│       ├── GetConversation/
│       └── GetMessageById/
│
├── Aegis.Modules.Messages.Infrastructure/
│   ├── Persistence/
│   │   ├── MessagesDbContext.cs
│   │   └── Repositories/
│   │       ├── MessageRepository.cs
│   │       └── SessionRepository.cs
│   └── SignalProtocol/
│       └── SignalProtocolService.cs
│
└── Aegis.Modules.Messages.API/
    ├── Controllers/
    │   └── MessagesController.cs
    └── Hubs/
        └── MessageHub.cs               # SignalR
```

**Key Domain Logic:**

```csharp
// Domain/Entities/Message.cs
public class Message : AggregateRoot<Guid>
{
    public Guid SenderId { get; private set; }
    public Guid RecipientId { get; private set; }
    public EncryptedContent Content { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public static Result<Message> Create(
        Guid senderId,
        Guid recipientId,
        EncryptedContent content)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            Status = MessageStatus.Pending,
            SentAt = DateTime.UtcNow
        };

        message.RaiseDomainEvent(new MessageSentEvent(
            message.Id, senderId, recipientId, message.SentAt));

        return Result.Success(message);
    }

    public Result MarkAsDelivered()
    {
        if (Status == MessageStatus.Read)
            return Result.Failure("Message already read");

        Status = MessageStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;

        RaiseDomainEvent(new MessageDeliveredEvent(Id, RecipientId));
        return Result.Success();
    }
}
```

---

### Module: Users

**Responsibilities:**
- User profile management
- Contact management
- User settings
- Presence (online/offline)

```
Modules/Users/
├── Domain/
│   ├── Entities/
│   │   ├── UserProfile.cs
│   │   └── Contact.cs
│   └── ValueObjects/
│       ├── DisplayName.cs
│       └── StatusMessage.cs
├── Application/
├── Infrastructure/
└── API/
```

---

### Module: Groups

**Responsibilities:**
- Group chat creation
- Member management
- Group settings
- Sender key distribution

```
Modules/Groups/
├── Domain/
│   ├── Entities/
│   │   ├── Group.cs
│   │   └── GroupMember.cs
│   └── ValueObjects/
│       └── GroupName.cs
├── Application/
├── Infrastructure/
└── API/
```

---

### Module: Files

**Responsibilities:**
- File upload/download
- Encryption/decryption
- Storage management

```
Modules/Files/
├── Domain/
│   └── Entities/
│       └── FileAttachment.cs
├── Application/
├── Infrastructure/
│   └── Storage/
│       ├── LocalFileStorage.cs
│       └── AzureBlobStorage.cs
└── API/
```

---

## 🗄️ Data Architecture

### Database Per Module

```
┌─────────────────┐
│  Auth Module    │ ──► AuthDb (Users, RefreshTokens)
└─────────────────┘

┌─────────────────┐
│ Messages Module │ ──► MessagesDb (Messages, Sessions)
└─────────────────┘

┌─────────────────┐
│  Users Module   │ ──► UsersDb (Profiles, Contacts)
└─────────────────┘

┌─────────────────┐
│ Groups Module   │ ──► GroupsDb (Groups, Members)
└─────────────────┘

┌─────────────────┐
│  Files Module   │ ──► FilesDb (Attachments) + Blob Storage
└─────────────────┘
```

### DbContext Per Module

```csharp
// Auth Module
public class AuthDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

// Messages Module
public class MessagesDbContext : DbContext
{
    public DbSet<Message> Messages { get; set; }
    public DbSet<EncryptionSession> Sessions { get; set; }
}
```

---

## 🔄 Communication Patterns

### 1. **Intra-Module Communication**
- Direct method calls
- Dependency injection

### 2. **Inter-Module Communication**
- **Domain Events** (for eventual consistency)
- **Event Bus** (in-memory → RabbitMQ for microservices)

```csharp
// Example: When user registers, create profile
public class UserRegisteredEventHandler
    : IEventHandler<UserRegisteredEvent>
{
    private readonly IUserProfileService _profileService;

    public async Task HandleAsync(UserRegisteredEvent @event)
    {
        // Create user profile in Users module
        await _profileService.CreateProfileAsync(
            @event.UserId,
            @event.Username);
    }
}
```

### 3. **Client-Server Communication**
- **REST API** for CRUD operations
- **SignalR** for real-time messaging
- **gRPC** (future) for inter-service communication

---

## 🚀 Migration Plan

### Phase 1: Create New Structure (Week 1)
```bash
# 1. Create directory structure
# 2. Create Shared libraries
# 3. Set up module projects
# 4. Configure solution file
```

### Phase 2: Move Shared Code (Week 1-2)
```bash
# 1. Move cryptography to Aegis.Shared.Cryptography
# 2. Move contracts to Aegis.Shared.Contracts
# 3. Move infrastructure to Aegis.Shared.Infrastructure
# 4. Create kernel primitives
```

### Phase 3: Create Modules (Week 2-4)
```bash
# 1. Auth module
# 2. Messages module
# 3. Users module
# 4. Groups module
# 5. Files module
```

### Phase 4: Migrate Clients (Week 4-5)
```bash
# 1. Create Clients.Shared
# 2. Refactor Desktop client
# 3. Refactor Android client
```

### Phase 5: Testing & Documentation (Week 5-6)
```bash
# 1. Unit tests per module
# 2. Integration tests
# 3. Architecture tests
# 4. Update documentation
```

---

## 📏 Architecture Tests

**Enforce architectural rules with NetArchTest:**

```csharp
public class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Message).Assembly)
            .That()
            .ResideInNamespace("Domain")
            .ShouldNot()
            .HaveDependencyOn("Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Modules_Should_Not_Reference_Other_Modules()
    {
        var messagesAssembly = typeof(Message).Assembly;
        var authAssembly = typeof(User).Assembly;

        var result = Types.InAssembly(messagesAssembly)
            .ShouldNot()
            .HaveDependencyOn(authAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
```

---

## 🎯 Benefits

### ✅ Modularity
- Clear boundaries
- Independent development
- Easy to understand

### ✅ Scalability
- Scale modules independently
- Extract to microservices easily
- Database per module

### ✅ Maintainability
- Loose coupling
- High cohesion
- SOLID principles

### ✅ Testability
- Easy to unit test
- Integration tests per module
- Architecture enforcement

### ✅ Team Organization
- Teams can own modules
- Parallel development
- Less conflicts

---

**Next Steps:** Implement this architecture!
