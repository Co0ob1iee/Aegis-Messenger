using Aegis.Modules.Security.Application.Alerting;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Aegis.Modules.Security.Infrastructure.Alerting;

/// <summary>
/// Sends security alerts via email using SMTP
/// </summary>
public sealed class EmailAlertingService : IAlertingService
{
    private readonly AlertingOptions.EmailOptions _options;
    private readonly ILogger<EmailAlertingService> _logger;

    public EmailAlertingService(
        IOptions<AlertingOptions> options,
        ILogger<EmailAlertingService> logger)
    {
        _options = options.Value.Email ?? new AlertingOptions.EmailOptions();
        _logger = logger;
    }

    public bool IsEnabled()
    {
        return _options.Enabled &&
               !string.IsNullOrEmpty(_options.SmtpServer) &&
               !string.IsNullOrEmpty(_options.FromAddress) &&
               _options.ToAddresses.Any();
    }

    public async Task SendAlertAsync(SecurityAlert alert, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled())
        {
            _logger.LogDebug("Email alerting is disabled or not configured. Skipping alert.");
            return;
        }

        try
        {
            var message = CreateEmailMessage(alert);
            await SendEmailAsync(message, cancellationToken);

            _logger.LogInformation(
                "Security alert email sent successfully: {EventType} ({Severity})",
                alert.EventType,
                alert.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send security alert email: {EventType} ({Severity})",
                alert.EventType,
                alert.Severity);
        }
    }

    private MimeMessage CreateEmailMessage(SecurityAlert alert)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

        foreach (var toAddress in _options.ToAddresses)
        {
            message.To.Add(MailboxAddress.Parse(toAddress));
        }

        message.Subject = alert.GetTitle();

        var bodyBuilder = new BodyBuilder
        {
            TextBody = CreateTextBody(alert),
            HtmlBody = CreateHtmlBody(alert)
        };

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private string CreateTextBody(SecurityAlert alert)
    {
        var lines = new List<string>
        {
            "AEGIS MESSENGER - SECURITY ALERT",
            "=================================",
            "",
            $"Event Type: {alert.EventType}",
            $"Severity: {alert.Severity}",
            $"Status: {(alert.IsSuccessful ? "Success" : "Failed")}",
            $"Timestamp: {alert.Timestamp:yyyy-MM-dd HH:mm:ss} UTC",
            $"Event ID: {alert.EventId}",
            ""
        };

        if (alert.UserId.HasValue)
            lines.Add($"User ID: {alert.UserId.Value}");

        if (!string.IsNullOrEmpty(alert.Username))
            lines.Add($"Username: {alert.Username}");

        if (!string.IsNullOrEmpty(alert.IpAddress))
            lines.Add($"IP Address: {alert.IpAddress}");

        if (!string.IsNullOrEmpty(alert.UserAgent))
            lines.Add($"User Agent: {alert.UserAgent}");

        if (!string.IsNullOrEmpty(alert.ErrorMessage))
        {
            lines.Add("");
            lines.Add("Error:");
            lines.Add(alert.ErrorMessage);
        }

        if (!string.IsNullOrEmpty(alert.Details))
        {
            lines.Add("");
            lines.Add("Details:");
            lines.Add(alert.Details);
        }

        lines.Add("");
        lines.Add("---");
        lines.Add("This is an automated security alert from Aegis Messenger.");

        return string.Join("\n", lines);
    }

    private string CreateHtmlBody(SecurityAlert alert)
    {
        var severityColor = alert.Severity switch
        {
            Domain.Enums.SecurityEventSeverity.Critical => "#dc3545",
            Domain.Enums.SecurityEventSeverity.High => "#fd7e14",
            Domain.Enums.SecurityEventSeverity.Medium => "#ffc107",
            Domain.Enums.SecurityEventSeverity.Low => "#0dcaf0",
            _ => "#6c757d"
        };

        var statusColor = alert.IsSuccessful ? "#28a745" : "#dc3545";
        var statusText = alert.IsSuccessful ? "✅ Success" : "❌ Failed";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {severityColor}; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }}
        .field {{ margin-bottom: 10px; }}
        .label {{ font-weight: bold; color: #495057; }}
        .value {{ color: #212529; }}
        .status {{ display: inline-block; padding: 5px 10px; border-radius: 3px; background-color: {statusColor}; color: white; }}
        .footer {{ background-color: #e9ecef; padding: 15px; text-align: center; font-size: 0.9em; color: #6c757d; border-radius: 0 0 5px 5px; }}
        .error {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 10px; margin-top: 10px; border-radius: 3px; }}
        .details {{ background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 10px; margin-top: 10px; border-radius: 3px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>🛡️ Aegis Messenger - Security Alert</h2>
        </div>
        <div class='content'>
            <div class='field'>
                <span class='label'>Event Type:</span>
                <span class='value'>{alert.EventType}</span>
            </div>
            <div class='field'>
                <span class='label'>Severity:</span>
                <span class='status' style='background-color: {severityColor};'>{alert.Severity}</span>
            </div>
            <div class='field'>
                <span class='label'>Status:</span>
                <span class='status' style='background-color: {statusColor};'>{statusText}</span>
            </div>
            <div class='field'>
                <span class='label'>Timestamp:</span>
                <span class='value'>{alert.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</span>
            </div>
            <div class='field'>
                <span class='label'>Event ID:</span>
                <span class='value'>{alert.EventId}</span>
            </div>
            {(alert.UserId.HasValue ? $@"
            <div class='field'>
                <span class='label'>User ID:</span>
                <span class='value'>{alert.UserId.Value}</span>
            </div>" : "")}
            {(!string.IsNullOrEmpty(alert.Username) ? $@"
            <div class='field'>
                <span class='label'>Username:</span>
                <span class='value'>{alert.Username}</span>
            </div>" : "")}
            {(!string.IsNullOrEmpty(alert.IpAddress) ? $@"
            <div class='field'>
                <span class='label'>IP Address:</span>
                <span class='value'>{alert.IpAddress}</span>
            </div>" : "")}
            {(!string.IsNullOrEmpty(alert.ErrorMessage) ? $@"
            <div class='error'>
                <strong>Error:</strong><br>
                {System.Net.WebUtility.HtmlEncode(alert.ErrorMessage)}
            </div>" : "")}
            {(!string.IsNullOrEmpty(alert.Details) ? $@"
            <div class='details'>
                <strong>Details:</strong><br>
                {System.Net.WebUtility.HtmlEncode(alert.Details)}
            </div>" : "")}
        </div>
        <div class='footer'>
            This is an automated security alert from Aegis Messenger.
        </div>
    </div>
</body>
</html>";

        return html;
    }

    private async Task SendEmailAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        try
        {
            // Connect to SMTP server
            var secureSocketOptions = _options.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _options.SmtpServer,
                _options.SmtpPort,
                secureSocketOptions,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            // Send message
            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
