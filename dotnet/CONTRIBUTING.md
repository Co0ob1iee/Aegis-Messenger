# 🤝 Contributing to Aegis Messenger

Dziękujemy za zainteresowanie contribucją do Aegis Messenger! Ten dokument zawiera guidelines i best practices.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Security Guidelines](#security-guidelines)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Git Commit Messages](#git-commit-messages)

---

## 📜 Code of Conduct

### Our Standards

- **Respectful** - Treat everyone with respect
- **Collaborative** - Work together constructively
- **Professional** - Maintain professional conduct
- **Inclusive** - Welcome diverse perspectives

### Unacceptable Behavior

- Harassment or discrimination
- Trolling or inflammatory comments
- Personal attacks
- Publishing private information

---

## 🚀 Getting Started

### Prerequisites

```bash
# Install .NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# Install Visual Studio 2022
winget install Microsoft.VisualStudio.2022.Community

# Install SQL Server LocalDB
winget install Microsoft.SQLServer.2022.LocalDB

# Install Git
winget install Git.Git
```

### Fork & Clone

```bash
# Fork repository on GitHub
# Then clone your fork
git clone https://github.com/YOUR_USERNAME/Aegis-Messenger.git
cd Aegis-Messenger/dotnet

# Add upstream remote
git remote add upstream https://github.com/Co0ob1iee/Aegis-Messenger.git
```

### Build

```bash
# Restore dependencies
dotnet restore Aegis.sln

# Build solution
dotnet build Aegis.sln

# Run tests
dotnet test Aegis.sln
```

### Run Backend

```bash
cd src/Aegis.Backend

# Set up user secrets (first time only)
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"

# Run
dotnet run
```

---

## 🔄 Development Workflow

### Branch Strategy

```
main           - Production-ready code
├── develop    - Integration branch
├── feature/*  - New features
├── bugfix/*   - Bug fixes
├── hotfix/*   - Critical fixes
└── security/* - Security patches
```

### Creating a Feature Branch

```bash
# Update develop branch
git checkout develop
git pull upstream develop

# Create feature branch
git checkout -b feature/disappearing-messages

# Make changes...
git add .
git commit -m "feat: add disappearing messages"

# Push to your fork
git push origin feature/disappearing-messages
```

---

## 💻 Coding Standards

### C# Style Guide

#### Naming Conventions

```csharp
// ✅ GOOD
public class UserService { }
public interface IMessageRepository { }
private readonly ILogger<UserService> _logger;
public async Task<User> GetUserAsync(Guid userId) { }

// ❌ BAD
public class userService { }
public interface MessageRepository { }
private ILogger<UserService> logger;
public Task<User> getUser(Guid user_id) { }
```

#### File Organization

```csharp
// Order:
// 1. Using statements (grouped, alphabetical)
// 2. Namespace
// 3. Class documentation
// 4. Class definition
// 5. Fields (private, protected, public)
// 6. Constructors
// 7. Properties
// 8. Public methods
// 9. Protected methods
// 10. Private methods

using System;
using System.Threading.Tasks;
using Aegis.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aegis.Core.Services;

/// <summary>
/// Manages user authentication and authorization
/// </summary>
public class AuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IUserRepository _userRepo;

    public AuthService(
        ILogger<AuthService> logger,
        IUserRepository userRepo)
    {
        _logger = logger;
        _userRepo = userRepo;
    }

    public async Task<User> AuthenticateAsync(string username, string password)
    {
        // Implementation
    }

    private bool ValidatePassword(string password, string hash)
    {
        // Implementation
    }
}
```

#### XML Documentation

```csharp
/// <summary>
/// Encrypts a message using Signal Protocol
/// </summary>
/// <param name="recipientId">ID of the recipient</param>
/// <param name="plaintext">Plaintext message to encrypt</param>
/// <returns>Encrypted message as byte array</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when no session exists with recipient
/// </exception>
public async Task<byte[]> EncryptMessageAsync(Guid recipientId, string plaintext)
{
    // Implementation
}
```

#### Async/Await

```csharp
// ✅ GOOD
public async Task<User> GetUserAsync(Guid userId)
{
    return await _context.Users.FindAsync(userId);
}

// ❌ BAD (blocking)
public User GetUser(Guid userId)
{
    return _context.Users.Find(userId);
}

// ❌ BAD (async void)
public async void DeleteUser(Guid userId)
{
    await _context.Users.Remove(userId);
}
```

#### Error Handling

```csharp
// ✅ GOOD
public async Task<User> GetUserAsync(Guid userId)
{
    try
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException($"User {userId} not found");

        return user;
    }
    catch (DbException ex)
    {
        _logger.LogError(ex, "Database error getting user {UserId}", userId);
        throw new DataAccessException("Failed to retrieve user", ex);
    }
}

// ❌ BAD (swallowing exceptions)
public async Task<User> GetUserAsync(Guid userId)
{
    try
    {
        return await _userRepo.GetByIdAsync(userId);
    }
    catch
    {
        return null; // Lost error information!
    }
}
```

### EditorConfig

Create `.editorconfig` in solution root:

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.interfaces_should_be_pascal_case.severity = warning
dotnet_naming_rule.interfaces_should_be_pascal_case.symbols = interface
dotnet_naming_rule.interfaces_should_be_pascal_case.style = begins_with_i

# Code style
csharp_prefer_braces = true:warning
csharp_new_line_before_open_brace = all
csharp_space_after_keywords_in_control_flow_statements = true
```

### Code Analysis

Enable Roslyn analyzers:

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.507">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Roslynator.Analyzers" Version="4.7.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

---

## 🔒 Security Guidelines

### CRITICAL Rules

1. **NEVER commit secrets**
   ```bash
   # ✅ GOOD - Use User Secrets
   dotnet user-secrets set "Jwt:Key" "your-secret-key"

   # ❌ BAD - Hardcoded in appsettings.json
   {
     "Jwt": {
       "Key": "my-secret-key"
     }
   }
   ```

2. **ALWAYS validate input**
   ```csharp
   // ✅ GOOD
   [HttpPost]
   public async Task<IActionResult> Register([FromBody] RegisterRequest request)
   {
       var validator = new RegisterRequestValidator();
       var result = await validator.ValidateAsync(request);

       if (!result.IsValid)
           return BadRequest(result.Errors);

       // ...
   }
   ```

3. **ALWAYS use parameterized queries**
   ```csharp
   // ✅ GOOD
   var users = await _context.Users
       .Where(u => u.Username == username)
       .ToListAsync();

   // ❌ BAD - SQL Injection!
   var users = await _context.Users
       .FromSqlRaw($"SELECT * FROM Users WHERE Username = '{username}'")
       .ToListAsync();
   ```

4. **NEVER log sensitive data**
   ```csharp
   // ✅ GOOD
   _logger.LogInformation("User {UserId} logged in", userId);

   // ❌ BAD - Logs password!
   _logger.LogInformation("User {Username} logged in with password {Password}",
       username, password);
   ```

### Security Checklist

Before submitting PR:

- [ ] No secrets in code
- [ ] Input validation implemented
- [ ] SQL injection prevented
- [ ] XSS prevented
- [ ] CSRF tokens used
- [ ] Authentication required
- [ ] Authorization checked
- [ ] Rate limiting applied
- [ ] Error messages sanitized
- [ ] Audit logging added

---

## 🧪 Testing Requirements

### Coverage Requirements

- **Minimum:** 80% code coverage
- **Target:** 90% code coverage
- **Critical paths:** 100% coverage

### Test Structure

```
tests/
├── Aegis.Core.Tests/
│   ├── Unit/              # Unit tests (fast, isolated)
│   ├── Integration/       # Integration tests (slower, DB)
│   └── Fixtures/          # Test fixtures and helpers
```

### Unit Test Example

```csharp
public class EncryptionServiceTests
{
    private readonly EncryptionService _service;

    public EncryptionServiceTests()
    {
        var logger = new Mock<ILogger<EncryptionService>>();
        _service = new EncryptionService(logger.Object);
    }

    [Fact]
    public async Task EncryptAsync_ValidInput_ReturnsEncryptedData()
    {
        // Arrange
        var plaintext = Encoding.UTF8.GetBytes("Hello, World!");
        var key = _service.GenerateKey();

        // Act
        var encrypted = await _service.EncryptAsync(plaintext, key);

        // Assert
        encrypted.Should().NotBeNull();
        encrypted.Should().NotBeEquivalentTo(plaintext);
    }

    [Fact]
    public async Task DecryptAsync_EncryptedData_ReturnsOriginalPlaintext()
    {
        // Arrange
        var plaintext = Encoding.UTF8.GetBytes("Hello, World!");
        var key = _service.GenerateKey();
        var encrypted = await _service.EncryptAsync(plaintext, key);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted, key);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public async Task DecryptAsync_WrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var plaintext = Encoding.UTF8.GetBytes("Hello, World!");
        var key = _service.GenerateKey();
        var wrongKey = _service.GenerateKey();
        var encrypted = await _service.EncryptAsync(plaintext, key);

        // Act & Assert
        await Assert.ThrowsAsync<CryptographicException>(
            () => _service.DecryptAsync(encrypted, wrongKey));
    }
}
```

### Integration Test Example

```csharp
public class MessageRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly AegisDbContext _context;
    private readonly MessageRepository _repository;

    public MessageRepositoryTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        var logger = new Mock<ILogger<MessageRepository>>();
        _repository = new MessageRepository(_context, logger.Object);
    }

    [Fact]
    public async Task InsertAsync_ValidMessage_PersistsToDatabase()
    {
        // Arrange
        var message = new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            EncryptedContent = new byte[] { 1, 2, 3 }
        };

        // Act
        var result = await _repository.InsertAsync(message);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        var retrieved = await _repository.GetByIdAsync(result.Id);
        retrieved.Should().NotBeNull();
        retrieved.EncryptedContent.Should().BeEquivalentTo(message.EncryptedContent);
    }
}
```

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

---

## 📝 Pull Request Process

### Before Creating PR

1. **Update from upstream**
   ```bash
   git fetch upstream
   git rebase upstream/develop
   ```

2. **Run tests**
   ```bash
   dotnet test
   ```

3. **Run code analysis**
   ```bash
   dotnet format
   dotnet build /p:TreatWarningsAsErrors=true
   ```

4. **Check security**
   ```bash
   dotnet list package --vulnerable
   ```

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Security fix
- [ ] Documentation update

## Related Issues
Closes #123

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Security Checklist
- [ ] No secrets committed
- [ ] Input validation implemented
- [ ] Security tests added

## Screenshots (if applicable)

## Additional Notes
```

### Code Review Criteria

✅ **Approved if:**
- All tests passing
- Code coverage ≥ 80%
- No security vulnerabilities
- Follows coding standards
- Documentation updated
- Approved by 2+ reviewers

❌ **Request changes if:**
- Tests failing
- Security issues found
- Code style violations
- Missing documentation
- Breaking changes without migration

---

## 📋 Git Commit Messages

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style (formatting)
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks
- `security`: Security fixes
- `perf`: Performance improvements

### Examples

```bash
# Feature
git commit -m "feat(auth): add two-factor authentication"

# Bug fix
git commit -m "fix(messages): resolve message ordering issue

Messages were not displaying in correct chronological order
due to timezone conversion bug.

Closes #456"

# Security fix
git commit -m "security(jwt): remove hardcoded JWT secret

CRITICAL: JWT secret was hardcoded in Program.cs
Now requires configuration via User Secrets or Key Vault

BREAKING CHANGE: Jwt:Key must be configured before startup"
```

### Commit Message Rules

1. Use present tense ("add feature" not "added feature")
2. Use imperative mood ("move cursor to..." not "moves cursor to...")
3. Limit first line to 72 characters
4. Reference issues and PRs in footer
5. Explain WHAT and WHY, not HOW

---

## 🎯 Project Structure

### Adding New Features

1. **Create models** in `Aegis.Core/Models/`
2. **Create interfaces** in `Aegis.Core/Interfaces/`
3. **Create entities** in `Aegis.Data/Entities/`
4. **Create repositories** in `Aegis.Data/Repositories/`
5. **Create controllers** in `Aegis.Backend/Controllers/`
6. **Add tests** in `tests/`
7. **Update documentation**

### Example: Adding Message Reactions

```bash
# 1. Create model
touch src/Aegis.Core/Models/MessageReaction.cs

# 2. Create entity
touch src/Aegis.Data/Entities/MessageReactionEntity.cs

# 3. Create repository
touch src/Aegis.Data/Repositories/ReactionRepository.cs

# 4. Create migration
cd src/Aegis.Data
dotnet ef migrations add AddMessageReactions

# 5. Create controller
touch src/Aegis.Backend/Controllers/ReactionsController.cs

# 6. Add tests
touch tests/Aegis.Core.Tests/Models/MessageReactionTests.cs
```

---

## 📚 Additional Resources

- [Development Roadmap](DEVELOPMENT_ROADMAP.md)
- [Security Audit](SECURITY_AUDIT.md)
- [API Documentation](docs/API.md)
- [Architecture Overview](docs/Architecture.md)

---

## 🆘 Getting Help

- **GitHub Issues:** https://github.com/Co0ob1iee/Aegis-Messenger/issues
- **Discussions:** https://github.com/Co0ob1iee/Aegis-Messenger/discussions
- **Security:** security@aegismessenger.com (private)

---

**Thank you for contributing to Aegis Messenger! 🎉**
