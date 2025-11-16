using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aegis.Desktop.ViewModels;
using Aegis.Desktop.Services;

namespace Aegis.Desktop;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public IHost Host { get; }

    public App()
    {
        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<ChatViewModel>();

                // Services
                services.AddSingleton<ApiService>();
                services.AddSingleton<SignalRService>();
                services.AddSingleton<LocalStorageService>();
            })
            .Build();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window? m_window;
}
