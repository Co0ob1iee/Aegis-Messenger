using Aegis.Clients.Desktop.Services;
using Aegis.Clients.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;

namespace Aegis.Clients.Desktop;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private static IServiceProvider? _serviceProvider;
    private Window? _mainWindow;

    public App()
    {
        this.InitializeComponent();
        ConfigureServices();
    }

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Services
        services.AddSingleton<IPrivacySettingsService, PrivacySettingsService>();

        // ViewModels
        services.AddTransient<PrivacySettingsViewModel>();

        // Add Aegis cryptography services (from Shared.Cryptography)
        // services.AddCryptographyServices();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Get service from dependency injection container
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider not initialized");
        }

        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
