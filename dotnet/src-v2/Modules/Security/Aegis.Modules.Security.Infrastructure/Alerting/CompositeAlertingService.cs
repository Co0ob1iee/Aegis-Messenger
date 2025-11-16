using Aegis.Modules.Security.Application.Alerting;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.Infrastructure.Alerting;

/// <summary>
/// Composite alerting service that sends alerts via both email and webhooks
/// </summary>
public sealed class CompositeAlertingService : IAlertingService
{
    private readonly EmailAlertingService _emailService;
    private readonly WebhookAlertingService _webhookService;
    private readonly ILogger<CompositeAlertingService> _logger;

    public CompositeAlertingService(
        EmailAlertingService emailService,
        WebhookAlertingService webhookService,
        ILogger<CompositeAlertingService> logger)
    {
        _emailService = emailService;
        _webhookService = webhookService;
        _logger = logger;
    }

    public bool IsEnabled()
    {
        return _emailService.IsEnabled() || _webhookService.IsEnabled();
    }

    public async Task SendAlertAsync(SecurityAlert alert, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled())
        {
            _logger.LogDebug("Alerting is completely disabled. Skipping alert.");
            return;
        }

        _logger.LogInformation(
            "Sending security alert: {EventType} ({Severity})",
            alert.EventType,
            alert.Severity);

        // Send both email and webhook alerts in parallel
        var tasks = new List<Task>();

        if (_emailService.IsEnabled())
            tasks.Add(_emailService.SendAlertAsync(alert, cancellationToken));

        if (_webhookService.IsEnabled())
            tasks.Add(_webhookService.SendAlertAsync(alert, cancellationToken));

        await Task.WhenAll(tasks);
    }
}
