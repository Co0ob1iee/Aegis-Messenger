namespace Aegis.Modules.Security.Application.Alerting;

/// <summary>
/// Service for sending security alerts via email and webhooks
/// </summary>
public interface IAlertingService
{
    /// <summary>
    /// Sends an alert for a security event
    /// </summary>
    Task SendAlertAsync(SecurityAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if alerting is enabled and configured
    /// </summary>
    bool IsEnabled();
}
