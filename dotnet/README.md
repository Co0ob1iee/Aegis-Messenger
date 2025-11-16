# Aegis Messenger - .NET Cross-Platform

> Bezpieczny komunikator z szyfrowaniem end-to-end dla **Windows** i **Android**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3-0078D4?logo=windows)](https://docs.microsoft.com/windows/apps/winui/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/maui)
[![Signal Protocol](https://img.shields.io/badge/Signal-Protocol-2592E9?logo=signal)](https://signal.org/docs/)

## 📋 Spis Treści

- [O Projekcie](#o-projekcie)
- [Architektura](#architektura)
- [Funkcje Bezpieczeństwa](#funkcje-bezpieczeństwa)
- [Wymagania](#wymagania)
- [Instalacja](#instalacja)
- [Struktura Projektu](#struktura-projektu)
- [Konfiguracja](#konfiguracja)
- [Uruchomienie](#uruchomienie)
- [API Documentation](#api-documentation)
- [Technologie](#technologie)
- [Development & Contributing](#development--contributing)
- [Security](#security)

---

## 🎯 O Projekcie

Aegis Messenger to **pełny port** oryginalnej aplikacji Android (Kotlin/Java) na platformy **Windows** i **Android** wykorzystując **.NET 8** i nowoczesne frameworki:

- **Windows Desktop**: WinUI 3
- **Android**: .NET MAUI
- **Backend**: ASP.NET Core 8.0 + SignalR
- **Database**: SQL Server + Entity Framework Core

### ✨ Kluczowe Funkcje

- 🔐 **End-to-End Encryption** - Signal Protocol (X3DH + Double Ratchet)
- 💬 **Czaty 1-on-1** - Bezpieczne rozmowy prywatne
- 👥 **Czaty Grupowe** - Szyfrowanie Sender Key
- 📎 **Przesyłanie Plików** - Zaszyfrowane załączniki
- 🔒 **Sealed Sender** - Anonimowość nadawcy
- 🛡️ **Anti-Debug** - Detekcja debugowania
- 📱 **Cross-Platform** - Windows + Android
- ⚡ **Real-time** - SignalR WebSocket

---

## 🏗️ Architektura

```
┌─────────────────────────────────────────────────────────┐
│                    Aegis Messenger                       │
│                    .NET Solution                         │
└─────────────────────────────────────────────────────────┘

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  Aegis.Desktop   │  │  Aegis.Android   │  │  Aegis.Backend   │
│    (WinUI 3)     │  │   (.NET MAUI)    │  │  (ASP.NET Core)  │
│                  │  │                  │  │                  │
│  • MVVM Pattern  │  │  • MVVM Pattern  │  │  • Web API       │
│  • XAML UI       │  │  • XAML UI       │  │  • SignalR Hub   │
│  • SignalR       │  │  • SignalR       │  │  • JWT Auth      │
│  • DPAPI         │  │  • Keystore      │  │  • EF Core       │
└────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘
         │                     │                     │
         └─────────────────────┴─────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │    Aegis.Core       │
                    │  (Shared Library)   │
                    │                     │
                    │  • Signal Protocol  │
                    │  • Cryptography     │
                    │  • Models           │
                    │  • Interfaces       │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │    Aegis.Data       │
                    │  (Data Layer)       │
                    │                     │
                    │  • EF Core          │
                    │  • Repositories     │
                    │  • SQL Server       │
                    └─────────────────────┘
```

---

## 🔒 Funkcje Bezpieczeństwa

### Signal Protocol Implementation

| Komponent | Implementacja |
|-----------|---------------|
| **X3DH Key Agreement** | Inicjalizacja sesji szyfrowania |
| **Double Ratchet** | Forward secrecy + self-healing |
| **Pre-Key Bundles** | Asynchroniczna wymiana kluczy |
| **Identity Keys** | Długoterminowe klucze użytkownika |
| **Safety Numbers** | Weryfikacja tożsamości (QR code) |

### Encryption Layers

```
┌─────────────────────────────────────────────┐
│  Application Layer                          │
│  • Signal Protocol (E2E Encryption)         │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│  Transport Layer                            │
│  • TLS 1.3 (HTTPS)                          │
│  • SignalR WebSocket Security               │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│  Storage Layer                              │
│  • Windows: DPAPI + AES-256-GCM             │
│  • Android: Keystore + SQLCipher            │
│  • SQL Server: Transparent Data Encryption  │
└─────────────────────────────────────────────┘
```

### Security Features

- ✅ **Root/Admin Detection** - Wykrywanie uprawnień root/administratora
- ✅ **Anti-Debug** - Detekcja debuggera (P/Invoke + timing)
- ✅ **Duress PIN** - PIN alarmowy do bezpiecznego wyczyszczenia danych
- ✅ **Sealed Sender** - Ukrywanie metadanych nadawcy
- ✅ **Forward Secrecy** - Kompromis klucza nie ujawnia poprzednich wiadomości
- ✅ **Self-Healing** - Automatyczne naprawianie po kompromisie

---

## 💻 Wymagania

### Development

- **Windows 11** (22H2 lub nowszy) dla WinUI 3
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (17.8+) z:
  - Workload: `.NET desktop development`
  - Workload: `.NET Multi-platform App UI development`
  - Workload: `ASP.NET and web development`
- **SQL Server 2022** (LocalDB, Express lub Developer)
- **Android SDK** (API 23+) dla .NET MAUI

### Runtime

| Platform | Minimum | Recommended |
|----------|---------|-------------|
| **Windows** | Windows 10 1809+ | Windows 11 22H2+ |
| **Android** | Android 6.0 (API 23) | Android 13+ (API 33) |
| **.NET Runtime** | .NET 8.0 | .NET 8.0 |

---

## 📦 Instalacja

### 1. Klonowanie repozytorium

```bash
git clone https://github.com/Co0ob1iee/Aegis-Messenger.git
cd Aegis-Messenger/dotnet
```

### 2. Przywracanie pakietów NuGet

```bash
dotnet restore Aegis.sln
```

### 3. Konfiguracja bazy danych

#### Opcja A: SQL Server LocalDB (Development)

```bash
# Migracje zostaną zastosowane automatycznie przy pierwszym uruchomieniu
dotnet ef database update --project src/Aegis.Backend
```

#### Opcja B: SQL Server (Production)

Edytuj `src/Aegis.Backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AegisDatabase": "Server=YOUR_SERVER;Database=AegisMessenger;User Id=aegis;Password=YOUR_PASSWORD;Encrypt=true;TrustServerCertificate=false"
  }
}
```

### 4. Konfiguracja JWT

Wygeneruj bezpieczny klucz JWT (minimum 32 znaki):

```bash
# PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | % {[char]$_})
```

Edytuj `src/Aegis.Backend/appsettings.json`:

```json
{
  "Jwt": {
    "Key": "YOUR_GENERATED_KEY_HERE",
    "Issuer": "AegisMessenger",
    "Audience": "AegisMessengerClients",
    "ExpiryHours": 24
  }
}
```

---

## 📁 Struktura Projektu

```
Aegis-Messenger/
├── dotnet/
│   ├── Aegis.sln                    # Visual Studio Solution
│   │
│   ├── src/
│   │   ├── Aegis.Core/              # 📚 Biblioteka wspólna
│   │   │   ├── Models/              # Modele danych
│   │   │   ├── Cryptography/        # Signal Protocol + AES
│   │   │   │   └── SignalProtocol/
│   │   │   │       ├── SignalSessionManager.cs
│   │   │   │       └── DoubleRatchet.cs
│   │   │   ├── Interfaces/          # Abstrakcje
│   │   │   └── Security/            # Root/Anti-Debug/Duress
│   │   │
│   │   ├── Aegis.Data/              # 💾 Warstwa danych
│   │   │   ├── Context/             # DbContext EF Core
│   │   │   ├── Entities/            # Encje bazodanowe
│   │   │   ├── Repositories/        # Implementacje repozytoriów
│   │   │   └── Migrations/          # Migracje EF Core
│   │   │
│   │   ├── Aegis.Backend/           # 🌐 ASP.NET Core API
│   │   │   ├── Controllers/         # REST API endpoints
│   │   │   │   ├── AuthController.cs
│   │   │   │   ├── MessagesController.cs
│   │   │   │   └── KeysController.cs
│   │   │   ├── Hubs/                # SignalR
│   │   │   │   └── MessageHub.cs
│   │   │   ├── Services/            # Logika biznesowa
│   │   │   │   ├── JwtService.cs
│   │   │   │   └── AuthService.cs
│   │   │   └── Program.cs           # Entry point
│   │   │
│   │   ├── Aegis.Desktop/           # 🖥️ WinUI 3 (Windows)
│   │   │   ├── Views/               # Widoki XAML
│   │   │   │   ├── MainWindow.xaml
│   │   │   │   ├── ChatView.xaml
│   │   │   │   └── LoginView.xaml
│   │   │   ├── ViewModels/          # MVVM ViewModels
│   │   │   │   ├── MainViewModel.cs
│   │   │   │   └── ChatViewModel.cs
│   │   │   ├── Services/            # SignalR Client, API
│   │   │   └── App.xaml             # Application
│   │   │
│   │   └── Aegis.Android/           # 📱 .NET MAUI (Android)
│   │       ├── Pages/               # Strony MAUI
│   │       │   ├── MainPage.xaml
│   │       │   └── ChatPage.xaml
│   │       ├── ViewModels/          # MVVM ViewModels
│   │       ├── Services/            # Android-specific
│   │       ├── Platforms/Android/   # Android kod natywny
│   │       └── MauiProgram.cs       # Entry point
│   │
│   ├── tests/
│   │   ├── Aegis.Core.Tests/        # Testy jednostkowe Core
│   │   └── Aegis.Backend.Tests/     # Testy integracyjne API
│   │
│   ├── docs/
│   │   ├── Architecture.md          # Dokumentacja architektury
│   │   ├── API.md                   # API Reference
│   │   └── Security.md              # Security Guide
│   │
│   └── README.md                    # Ten plik
```

---

## ⚙️ Konfiguracja

### appsettings.json (Backend)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AegisDatabase": "Server=(localdb)\\mssqllocaldb;Database=AegisMessenger;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS",
    "Issuer": "AegisMessenger",
    "Audience": "AegisMessengerClients",
    "ExpiryHours": 24
  },
  "SignalR": {
    "KeepAliveInterval": 15,
    "ClientTimeoutInterval": 30
  }
}
```

### Konfiguracja Windows (DPAPI)

Windows używa **Data Protection API (DPAPI)** dla szyfrowania lokalnych danych:

```csharp
// Automatycznie skonfigurowane w Aegis.Desktop
using System.Security.Cryptography;

var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
```

### Konfiguracja Android (Keystore)

Android używa **Android Keystore** dla bezpiecznego przechowywania kluczy:

```csharp
// Skonfigurowane w Aegis.Android/Platforms/Android
// Wykorzystuje Xamarin.AndroidX.Security.SecurityCrypto
```

---

## 🚀 Uruchomienie

### Backend (ASP.NET Core API)

```bash
cd src/Aegis.Backend
dotnet run
```

Swagger UI dostępny na: https://localhost:7001/swagger

### Windows Desktop (WinUI 3)

```bash
# Visual Studio
F5 (Debug) lub Ctrl+F5 (Release)

# CLI
cd src/Aegis.Desktop
dotnet run
```

### Android (.NET MAUI)

```bash
# Visual Studio
1. Podłącz urządzenie Android lub uruchom emulator
2. Ustaw Aegis.Android jako Startup Project
3. Naciśnij F5

# CLI
cd src/Aegis.Android
dotnet build -t:Run -f net8.0-android
```

---

## 📡 API Documentation

### Authentication Endpoints

#### POST /api/auth/register

Rejestracja nowego użytkownika.

**Request:**
```json
{
  "username": "user123",
  "password": "SecurePassword123!",
  "email": "user@example.com",
  "displayName": "John Doe"
}
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "user123",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### POST /api/auth/login

Logowanie użytkownika.

**Request:**
```json
{
  "username": "user123",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "user123",
  "displayName": "John Doe",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### SignalR Hub

#### Hub URL: /hubs/messages

**Methods:**

- `SendMessage(recipientId, encryptedContent, messageType)` - Wyślij wiadomość 1-on-1
- `SendGroupMessage(groupId, encryptedContent)` - Wyślij wiadomość grupową
- `JoinGroup(groupId)` - Dołącz do grupy
- `LeaveGroup(groupId)` - Opuść grupę
- `SendTypingIndicator(recipientId, isTyping)` - Wskaźnik pisania
- `MarkAsRead(senderId, messageId)` - Oznacz jako przeczytane

**Events (Client receives):**

- `ReceiveMessage` - Otrzymano wiadomość
- `ReceiveGroupMessage` - Otrzymano wiadomość grupową
- `UserOnline` - Użytkownik online
- `UserOffline` - Użytkownik offline
- `TypingIndicator` - Wskaźnik pisania
- `MessageRead` - Wiadomość przeczytana

---

## 🛠️ Technologie

### Backend

| Technologia | Wersja | Przeznaczenie |
|------------|--------|---------------|
| ASP.NET Core | 8.0 | Web API Framework |
| Entity Framework Core | 8.0 | ORM (SQL Server) |
| SignalR | 8.0 | Real-time WebSocket |
| JWT Bearer | 8.0 | Authentication |
| Serilog | 8.0 | Structured logging |
| Swashbuckle | 6.5 | OpenAPI/Swagger |

### Desktop (Windows)

| Technologia | Wersja | Przeznaczenie |
|------------|--------|---------------|
| WinUI 3 | 1.5 | Native Windows UI |
| Windows App SDK | 1.5 | Windows APIs |
| CommunityToolkit.Mvvm | 8.2 | MVVM framework |
| SignalR Client | 8.0 | Real-time client |
| SQLite | 8.0 | Local database |

### Mobile (Android)

| Technologia | Wersja | Przeznaczenie |
|------------|--------|---------------|
| .NET MAUI | 8.0 | Cross-platform UI |
| CommunityToolkit.Maui | 7.0 | MAUI controls |
| SignalR Client | 8.0 | Real-time client |
| SQLite PCL | 1.9 | Local database |
| AndroidX.Security | 1.1 | Keystore/EncryptedSharedPreferences |

### Core (Shared)

| Technologia | Wersja | Przeznaczenie |
|------------|--------|---------------|
| libsignal-protocol-dotnet | 2.3 | Signal Protocol |
| BouncyCastle | 1.9 | Cryptography |
| System.Security.Cryptography | 8.0 | AES-GCM, HKDF |

---

## 🧪 Testy

### Uruchomienie testów jednostkowych

```bash
# Wszystkie testy
dotnet test

# Tylko Aegis.Core
dotnet test tests/Aegis.Core.Tests/

# Z coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Testy integracyjne API

```bash
dotnet test tests/Aegis.Backend.Tests/
```

---

## 🚀 Development & Contributing

### 📖 Dokumentacja Rozwoju

- **[Modular Architecture](docs/MODULAR_ARCHITECTURE.md)** ⭐ **NEW!** - Enterprise-grade wielomodułowa architektura
  - Domain-Driven Design (DDD)
  - Clean Architecture
  - SOLID principles
  - Modular Monolith → Microservices ready
  - 28 projektów w nowej strukturze (`src-v2/`)

- **[Implementation Guide](docs/IMPLEMENTATION_GUIDE.md)** ⭐ **NEW!** - Przewodnik implementacji
  - Plan migracji (7 tygodni)
  - Przykłady kodu dla każdego modułu
  - Testing strategies
  - Timeline i milestones

- **[Development Roadmap](DEVELOPMENT_ROADMAP.md)** - Szczegółowy plan rozwoju projektu
  - Fazy rozwoju (1-4)
  - Nowe funkcjonalności (Disappearing messages, Voice/Video calls, etc.)
  - Roadmap na 4-5 miesięcy
  - Metryki sukcesu

- **[Contributing Guidelines](CONTRIBUTING.md)** - Jak kontrybuować do projektu
  - Code of Conduct
  - Coding standards i style guide
  - Testing requirements
  - Pull request process
  - Git commit message conventions

### 🤝 Jak Zacząć Kontrybuować?

```bash
# 1. Fork repozytorium
# 2. Clone swojego forka
git clone https://github.com/YOUR_USERNAME/Aegis-Messenger.git

# 3. Utwórz branch dla feature
git checkout -b feature/amazing-feature

# 4. Commit zmiany
git commit -m "feat: add amazing feature"

# 5. Push do forka
git push origin feature/amazing-feature

# 6. Otwórz Pull Request
```

Więcej informacji: [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🔒 Security

### 🛡️ Security Audit

**[Pełny Security Audit dostępny tutaj: SECURITY_AUDIT.md](SECURITY_AUDIT.md)**

#### Podsumowanie Obecnego Stanu

| Severity | Count | Status |
|----------|-------|--------|
| 🔴 **CRITICAL** | 4 | ⚠️ Wymaga natychmiastowej akcji |
| 🟠 **HIGH** | 7 | ⚠️ Fix w ciągu tygodnia |
| 🟡 **MEDIUM** | 8 | 📋 Fix w ciągu miesiąca |
| 🟢 **LOW** | 4 | ✅ Fix gdy będzie czas |

#### Krytyczne Problemy do Naprawy

1. **CRIT-001:** In-memory storage kluczy Signal Protocol
2. **CRIT-002:** Hardcoded JWT secret key
3. **CRIT-003:** Brak szyfrowania sesji w bazie danych
4. **HIGH-001:** Brak rate limiting (DoS vulnerability)

### 🐛 Zgłaszanie Luk Bezpieczeństwa

**NIE twórz publicznego issue dla luk bezpieczeństwa!**

Zamiast tego:
- Email: security@aegismessenger.com (private)
- Użyj [GitHub Security Advisories](https://github.com/Co0ob1iee/Aegis-Messenger/security/advisories)

Otrzymasz odpowiedź w ciągu 48 godzin.

### 🔐 Security Best Practices

Podczas rozwoju:
- ✅ **ZAWSZE** używaj User Secrets dla development
- ✅ **ZAWSZE** waliduj input od użytkownika
- ✅ **NIGDY** nie commituj secretów
- ✅ **ZAWSZE** używaj parameterized queries
- ✅ Śledź [Security Checklist](CONTRIBUTING.md#security-checklist)

---

## 📝 Licencja

Ten projekt jest portem edukacyjnym oryginalnej aplikacji Aegis Messenger.

---

## 🤝 Kontakt

Dla pytań technicznych lub wsparcia:
- GitHub Issues: [Create Issue](https://github.com/Co0ob1iee/Aegis-Messenger/issues)
- Security: security@aegismessenger.com (private)

---

## 🎓 Wykorzystane Źródła

- [Signal Protocol Specification](https://signal.org/docs/)
- [libsignal-protocol-dotnet](https://github.com/WhisperSystems/libsignal-protocol-dotnet)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [WinUI 3 Documentation](https://docs.microsoft.com/windows/apps/winui/)
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui/)

---

**Zbudowano z ❤️ używając .NET 8.0**
