# Aegis Messenger - Desktop Client (WinUI 3)

Modern Windows desktop application for Aegis Messenger built with **WinUI 3** and **Windows App SDK**.

## Overview

Aegis Desktop Client provides a native Windows 11 experience with Fluent Design, featuring:
- End-to-end encrypted messaging (Signal Protocol)
- Advanced privacy settings (message padding, timestamp fuzzing, sealed sender)
- Modern UI with dark/light theme support
- Native Windows notifications and integrations

## Architecture

### Technology Stack

- **WinUI 3** - Modern UI framework for Windows
- **Windows App SDK** - Latest Windows development platform
- **.NET 8** - Cross-platform runtime
- **MVVM Pattern** - Model-View-ViewModel architecture
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection

### Project Structure

```
Aegis.Clients.Desktop/
├── App.xaml / App.xaml.cs          # Application entry point + DI setup
├── MainWindow.xaml / .cs           # Main navigation window
├── Views/                          # XAML pages
│   ├── ConversationsPage.xaml      # Messages list (placeholder)
│   ├── ContactsPage.xaml           # Contacts list (placeholder)
│   ├── GroupsPage.xaml             # Groups list (placeholder)
│   ├── SettingsPage.xaml           # Settings with sub-navigation
│   └── PrivacySettingsPage.xaml    # ✅ Privacy Settings (COMPLETE)
├── ViewModels/                     # MVVM ViewModels
│   └── PrivacySettingsViewModel.cs # ✅ Privacy settings logic
├── Services/                       # Business logic services
│   └── PrivacySettingsService.cs   # ✅ Settings persistence (JSON)
├── Converters/                     # Value converters for XAML binding
│   └── BoolToVisibilityConverter.cs
└── Models/                         # Data models
```

## Privacy Settings Page ✅

**ZADANIE 7 COMPLETED** - Fully functional Privacy Settings UI

### Features

**Message Privacy:**
- ✅ **Message Padding** - Hide message length from traffic analysis
  - 3 strategies: Random (0-512 bytes), Fixed Block (256 bytes), Exponential (power of 2)
  - Toggle on/off with strategy selector

- ✅ **Timestamp Fuzzing** - Prevent timing analysis
  - Configurable delay range (1-300 seconds slider)
  - Random delays to hide message timing patterns

- ✅ **Sealed Sender** - NEW! Hide sender identity from server
  - Multi-layer encryption (ECDH + AES-GCM + Signal Protocol)
  - Server only sees recipient, not sender
  - Marked with "NEW" badge in UI

**Message Retention:**
- ✅ **Disappearing Messages** - Auto-delete after specified time
  - 9 timer options: 30s, 1m, 5m, 30m, 1h, 6h, 12h, 24h, 1 week
  - Default setting for new conversations
  - Per-conversation customization (future)

**Metadata Privacy:**
- ✅ **Read Receipts** - Control when others see you've read their messages
- ✅ **Typing Indicators** - Control when others see you're typing

### UI Components

**Modern Fluent Design:**
- Card-based layout with rounded corners
- Toggles for quick enable/disable
- Expandable sections (show options only when feature enabled)
- Sliders and dropdowns for configuration
- InfoBar for important privacy information
- Accent button for saving

**Responsive Layout:**
- Max width 800px for optimal readability
- Scrollable content
- Proper spacing (24px between sections)
- Consistent padding (40px horizontal, 24px vertical)

### Data Persistence

**Storage Location:** `%LOCALAPPDATA%\AegisMessenger\privacy-settings.json`

**Format:**
```json
{
  "MessagePaddingEnabled": false,
  "PaddingStrategy": "Random",
  "TimestampFuzzingEnabled": false,
  "TimestampFuzzingRangeSeconds": 60,
  "SealedSenderEnabled": true,
  "DisappearingMessagesEnabled": false,
  "DefaultDisappearingMessageTimer": "01:00:00",
  "ReadReceiptsEnabled": true,
  "TypingIndicatorsEnabled": true
}
```

**Defaults:**
- Message padding: **OFF** (user opt-in)
- Timestamp fuzzing: **OFF** (user opt-in)
- Sealed sender: **ON** (privacy by default)
- Disappearing messages: **OFF** (user opt-in)
- Read receipts: **ON** (standard messenger behavior)
- Typing indicators: **ON** (standard messenger behavior)

### Navigation

**Access Privacy Settings:**
1. Click **Settings** icon in main NavigationView
2. Select **Privacy** from Settings sub-menu
3. Configure privacy options
4. Click **Save Settings** button

**Navigation Hierarchy:**
```
MainWindow
  └── SettingsPage (sub-navigation)
        └── PrivacySettingsPage ✅
        └── NotificationsSettingsPage (TODO)
        └── AccountSettingsPage (TODO)
        └── AppearanceSettingsPage (TODO)
        └── StorageSettingsPage (TODO)
        └── AboutPage (TODO)
```

## MVVM Architecture

### PrivacySettingsViewModel

**Pattern:** MVVM with CommunityToolkit.Mvvm

**Features:**
- Two-way data binding (`x:Bind Mode=TwoWay`)
- Observable properties (`[ObservableProperty]`)
- Commands (`[RelayCommand]`)
- Automatic property change notifications

**Example:**
```csharp
[ObservableProperty]
private bool _isMessagePaddingEnabled;

[RelayCommand]
private async Task SaveAsync()
{
    await _privacySettings.SaveSettingsAsync(settings);
}
```

**Benefits:**
- Clean code (no INotifyPropertyChanged boilerplate)
- Type-safe bindings (`x:Bind` vs `Binding`)
- Compile-time validation
- Better performance

## Dependency Injection

**Services registered in App.xaml.cs:**

```csharp
services.AddSingleton<IPrivacySettingsService, PrivacySettingsService>();
services.AddTransient<PrivacySettingsViewModel>();

// Integration with Aegis backend (when needed):
// services.AddCryptographyServices(); // From Shared.Cryptography
```

**Usage in pages:**
```csharp
public PrivacySettingsPage()
{
    ViewModel = App.GetService<PrivacySettingsViewModel>();
}
```

## Value Converters

**BoolToVisibilityConverter:**
- Converts `bool` to `Visibility`
- `true` → `Visible`, `false` → `Collapsed`
- Used for conditional UI visibility

**Example:**
```xaml
<StackPanel
    Visibility="{x:Bind ViewModel.IsMessagePaddingEnabled, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}">
    <!-- Shown only when padding is enabled -->
</StackPanel>
```

## Building and Running

### Prerequisites

1. **Visual Studio 2022** (17.8+)
   - Workloads: .NET Desktop Development, Universal Windows Platform
   - Individual components: Windows App SDK C# Templates

2. **Windows 11** (recommended)
   - Windows 10 version 1809+ (minimum)

3. **.NET 8 SDK**

### Build

```bash
# Restore NuGet packages
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run
```

### Required NuGet Packages

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.x" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.x" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.x" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.x" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.x" />
```

## Integration with Aegis Backend

### Privacy Services Integration

To integrate with real Aegis privacy services:

1. **Add reference to Shared.Cryptography:**
```xml
<ProjectReference Include="../../Shared/Aegis.Shared.Cryptography/Aegis.Shared.Cryptography.csproj" />
```

2. **Register services in App.xaml.cs:**
```csharp
services.AddCryptographyServices(); // Adds MessagePadding, TimestampFuzzing, SealedSender
```

3. **Connect ViewModel to real services:**
```csharp
// In PrivacySettingsViewModel - replace JSON service with real implementations
private readonly IMessagePaddingService _messagePadding;
private readonly ITimestampFuzzingService _timestampFuzzing;
private readonly ISealedSenderService _sealedSender;
```

### Example Integration

**MessagePaddingService usage:**
```csharp
// When sending a message
if (settings.MessagePaddingEnabled)
{
    var paddedMessage = await _messagePadding.PadMessageAsync(
        originalMessage,
        strategy: settings.PaddingStrategy
    );
}
```

**TimestampFuzzingService usage:**
```csharp
// When creating timestamp
if (settings.TimestampFuzzingEnabled)
{
    var fuzzedTimestamp = _timestampFuzzing.FuzzTimestamp(
        originalTimestamp,
        maxOffsetSeconds: settings.TimestampFuzzingRangeSeconds
    );
}
```

**SealedSenderService usage:**
```csharp
// When sending message
if (settings.SealedSenderEnabled)
{
    var sealedMessage = await _sealedSender.CreateSealedMessageAsync(
        senderId,
        recipientId,
        plaintext,
        certificate
    );
}
```

## Screenshots

### Privacy Settings Page

![Privacy Settings - Message Privacy](docs/screenshots/privacy-message.png)
- Message Padding with 3 strategies
- Timestamp Fuzzing with configurable range
- Sealed Sender toggle (NEW badge)

![Privacy Settings - Message Retention](docs/screenshots/privacy-retention.png)
- Disappearing Messages with 9 timer options
- InfoBar explaining per-conversation customization

![Privacy Settings - Metadata Privacy](docs/screenshots/privacy-metadata.png)
- Read Receipts toggle
- Typing Indicators toggle

## Future Enhancements

**Planned Features:**
- [ ] Per-conversation privacy settings override
- [ ] Privacy presets (Paranoid, Balanced, Standard)
- [ ] Advanced sealed sender options (certificate validity)
- [ ] Network anonymity (Tor integration toggle)
- [ ] Screen security (prevent screenshots, screen sharing warnings)
- [ ] Incognito mode (disable all logs, history)

**Additional Settings Pages (ZADANIE 8-9):**
- [ ] Disappearing Messages UI with visual timer
- [ ] Notifications settings
- [ ] Account management
- [ ] Appearance/Theme customization
- [ ] Storage management
- [ ] About page

## Testing

### Manual Testing Checklist

**Privacy Settings:**
- [ ] Message Padding toggle works
- [ ] Padding strategy selection persists
- [ ] Timestamp Fuzzing slider updates value
- [ ] Sealed Sender toggle works
- [ ] Disappearing Messages timer selection works
- [ ] Read Receipts toggle works
- [ ] Typing Indicators toggle works
- [ ] Save button saves to JSON file
- [ ] Settings persist across app restarts
- [ ] Expandable sections show/hide correctly

### Unit Testing

**ViewModels:**
```csharp
[Fact]
public void PaddingStrategy_IndexMapping_ShouldBeCorrect()
{
    var viewModel = new PrivacySettingsViewModel(logger, service);

    // Test index → enum mapping
    Assert.Equal(PaddingStrategy.Random, viewModel.GetPaddingStrategy(0));
    Assert.Equal(PaddingStrategy.FixedBlock, viewModel.GetPaddingStrategy(1));
    Assert.Equal(PaddingStrategy.Exponential, viewModel.GetPaddingStrategy(2));
}

[Fact]
public async Task SaveSettings_ShouldPersistToStorage()
{
    var service = new Mock<IPrivacySettingsService>();
    var viewModel = new PrivacySettingsViewModel(logger, service.Object);

    viewModel.IsMessagePaddingEnabled = true;
    await viewModel.SaveCommand.ExecuteAsync(null);

    service.Verify(s => s.SaveSettingsAsync(It.IsAny<PrivacySettings>()), Times.Once);
}
```

**Services:**
```csharp
[Fact]
public async Task PrivacySettingsService_SaveAndLoad_ShouldRoundTrip()
{
    var service = new PrivacySettingsService(logger);

    var settings = new PrivacySettings
    {
        MessagePaddingEnabled = true,
        TimestampFuzzingRangeSeconds = 120
    };

    await service.SaveSettingsAsync(settings);
    var loaded = service.GetSettings();

    Assert.True(loaded.MessagePaddingEnabled);
    Assert.Equal(120, loaded.TimestampFuzzingRangeSeconds);
}
```

## Troubleshooting

### Settings Not Saving

**Problem:** Changes don't persist after restart

**Solutions:**
1. Check file permissions for `%LOCALAPPDATA%\AegisMessenger\`
2. Run app as administrator (once to create folder)
3. Check logs for serialization errors
4. Verify JSON file exists and is valid

### UI Not Updating

**Problem:** Changes in ViewModel don't update UI

**Solutions:**
1. Ensure `x:Bind Mode=TwoWay` is used for input controls
2. Check that ViewModel implements `INotifyPropertyChanged`
3. Use `[ObservableProperty]` attribute (CommunityToolkit.Mvvm)
4. Verify converter is registered in App.xaml resources

### Binding Errors

**Problem:** Crash or blank screen

**Solutions:**
1. Check Output window for binding errors
2. Verify property names match exactly (case-sensitive)
3. Ensure x:Bind properties are public
4. Use `Mode=OneWay` for readonly properties

## Performance

**Optimization:**
- **x:Bind** compiled bindings (faster than Binding)
- **Lazy loading** - Settings page only loads when accessed
- **Minimal re-renders** - ObservableProperty only notifies when changed
- **JSON caching** - Settings loaded once, cached in memory

**Memory Usage:**
- Application: ~50-80 MB
- PrivacySettingsViewModel: <1 MB
- Settings JSON file: <1 KB

## License

Part of Aegis Messenger - see main repository LICENSE.