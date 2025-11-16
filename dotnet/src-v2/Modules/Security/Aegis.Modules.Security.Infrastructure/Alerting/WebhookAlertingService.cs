using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Aegis.Modules.Security.Application.Alerting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Aegis.Modules.Security.Infrastructure.Alerting;

/// <summary>
/// Sends security alerts via webhooks (Slack, Discord, Microsoft Teams, Generic)
/// </summary>
public sealed class WebhookAlertingService : IAlertingService
{
    private readonly List<AlertingOptions.WebhookOptions> _webhooks;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookAlertingService> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public WebhookAlertingService(
        IOptions<AlertingOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookAlertingService> logger)
    {
        _webhooks = options.Value.Webhooks ?? new List<AlertingOptions.WebhookOptions>();
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Webhook request failed (attempt {RetryCount}/3). Retrying in {Delay}s...",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    public bool IsEnabled()
    {
        return _webhooks.Any(w => !string.IsNullOrEmpty(w.Url));
    }

    public async Task SendAlertAsync(SecurityAlert alert, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled())
        {
            _logger.LogDebug("Webhook alerting is disabled or not configured. Skipping alert.");
            return;
        }

        var tasks = _webhooks
            .Where(w => !string.IsNullOrEmpty(w.Url))
            .Select(webhook => SendWebhookAsync(webhook, alert, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task SendWebhookAsync(
        AlertingOptions.WebhookOptions webhook,
        SecurityAlert alert,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = CreatePayload(webhook.Type, alert);
            var httpClient = _httpClientFactory.CreateClient("SecurityAlerting");
            httpClient.Timeout = TimeSpan.FromSeconds(webhook.TimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            // Add custom headers
            foreach (var header in webhook.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Send with retry policy
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var clonedRequest = await CloneHttpRequestMessageAsync(request);
                return await httpClient.SendAsync(clonedRequest, cancellationToken);
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Security alert webhook sent successfully to {WebhookName}: {EventType} ({Severity})",
                    webhook.Name,
                    alert.EventType,
                    alert.Severity);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook {WebhookName} returned status code {StatusCode}",
                    webhook.Name,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send security alert webhook to {WebhookName}: {EventType} ({Severity})",
                webhook.Name,
                alert.EventType,
                alert.Severity);
        }
    }

    private string CreatePayload(AlertingOptions.WebhookType type, SecurityAlert alert)
    {
        return type switch
        {
            AlertingOptions.WebhookType.Slack => CreateSlackPayload(alert),
            AlertingOptions.WebhookType.Discord => CreateDiscordPayload(alert),
            AlertingOptions.WebhookType.MicrosoftTeams => CreateTeamsPayload(alert),
            _ => CreateGenericPayload(alert)
        };
    }

    private string CreateSlackPayload(SecurityAlert alert)
    {
        var color = alert.Severity switch
        {
            Domain.Enums.SecurityEventSeverity.Critical => "danger",
            Domain.Enums.SecurityEventSeverity.High => "warning",
            Domain.Enums.SecurityEventSeverity.Medium => "#ffc107",
            Domain.Enums.SecurityEventSeverity.Low => "#0dcaf0",
            _ => "good"
        };

        var fields = new List<object>
        {
            new { title = "Event Type", value = alert.EventType.ToString(), @short = true },
            new { title = "Severity", value = alert.Severity.ToString(), @short = true },
            new { title = "Status", value = alert.IsSuccessful ? "✅ Success" : "❌ Failed", @short = true },
            new { title = "Timestamp", value = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"), @short = true }
        };

        if (alert.UserId.HasValue)
            fields.Add(new { title = "User ID", value = alert.UserId.Value.ToString(), @short = true });

        if (!string.IsNullOrEmpty(alert.Username))
            fields.Add(new { title = "Username", value = alert.Username, @short = true });

        if (!string.IsNullOrEmpty(alert.IpAddress))
            fields.Add(new { title = "IP Address", value = alert.IpAddress, @short = true });

        if (!string.IsNullOrEmpty(alert.ErrorMessage))
            fields.Add(new { title = "Error", value = alert.ErrorMessage, @short = false });

        if (!string.IsNullOrEmpty(alert.Details))
            fields.Add(new { title = "Details", value = alert.Details, @short = false });

        var payload = new
        {
            username = "Aegis Security",
            icon_emoji = ":shield:",
            attachments = new[]
            {
                new
                {
                    color,
                    title = "🛡️ Security Alert",
                    text = alert.GetTitle(),
                    fields,
                    footer = "Aegis Messenger Security",
                    ts = new DateTimeOffset(alert.Timestamp).ToUnixTimeSeconds()
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private string CreateDiscordPayload(SecurityAlert alert)
    {
        var color = alert.Severity switch
        {
            Domain.Enums.SecurityEventSeverity.Critical => 0xDC3545, // Red
            Domain.Enums.SecurityEventSeverity.High => 0xFD7E14,     // Orange
            Domain.Enums.SecurityEventSeverity.Medium => 0xFFC107,   // Yellow
            Domain.Enums.SecurityEventSeverity.Low => 0x0DCAF0,      // Cyan
            _ => 0x6C757D                                             // Gray
        };

        var fields = new List<object>
        {
            new { name = "Event Type", value = alert.EventType.ToString(), inline = true },
            new { name = "Severity", value = alert.Severity.ToString(), inline = true },
            new { name = "Status", value = alert.IsSuccessful ? "✅ Success" : "❌ Failed", inline = true },
            new { name = "Timestamp", value = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"), inline = false }
        };

        if (alert.UserId.HasValue)
            fields.Add(new { name = "User ID", value = alert.UserId.Value.ToString(), inline = true });

        if (!string.IsNullOrEmpty(alert.Username))
            fields.Add(new { name = "Username", value = alert.Username, inline = true });

        if (!string.IsNullOrEmpty(alert.IpAddress))
            fields.Add(new { name = "IP Address", value = alert.IpAddress, inline = true });

        if (!string.IsNullOrEmpty(alert.ErrorMessage))
            fields.Add(new { name = "Error", value = alert.ErrorMessage, inline = false });

        if (!string.IsNullOrEmpty(alert.Details))
            fields.Add(new { name = "Details", value = alert.Details, inline = false });

        var payload = new
        {
            username = "Aegis Security",
            avatar_url = "https://via.placeholder.com/128/6C757D/FFFFFF?text=AS",
            embeds = new[]
            {
                new
                {
                    title = "🛡️ Security Alert",
                    description = alert.GetTitle(),
                    color,
                    fields,
                    footer = new
                    {
                        text = "Aegis Messenger Security"
                    },
                    timestamp = alert.Timestamp.ToString("o")
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private string CreateTeamsPayload(SecurityAlert alert)
    {
        var themeColor = alert.Severity switch
        {
            Domain.Enums.SecurityEventSeverity.Critical => "DC3545",
            Domain.Enums.SecurityEventSeverity.High => "FD7E14",
            Domain.Enums.SecurityEventSeverity.Medium => "FFC107",
            Domain.Enums.SecurityEventSeverity.Low => "0DCAF0",
            _ => "6C757D"
        };

        var facts = new List<object>
        {
            new { name = "Event Type", value = alert.EventType.ToString() },
            new { name = "Severity", value = alert.Severity.ToString() },
            new { name = "Status", value = alert.IsSuccessful ? "✅ Success" : "❌ Failed" },
            new { name = "Timestamp", value = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC") }
        };

        if (alert.UserId.HasValue)
            facts.Add(new { name = "User ID", value = alert.UserId.Value.ToString() });

        if (!string.IsNullOrEmpty(alert.Username))
            facts.Add(new { name = "Username", value = alert.Username });

        if (!string.IsNullOrEmpty(alert.IpAddress))
            facts.Add(new { name = "IP Address", value = alert.IpAddress });

        var sections = new List<object>
        {
            new
            {
                activityTitle = "🛡️ Aegis Messenger - Security Alert",
                activitySubtitle = alert.GetTitle(),
                facts
            }
        };

        if (!string.IsNullOrEmpty(alert.ErrorMessage))
        {
            sections.Add(new
            {
                title = "Error",
                text = alert.ErrorMessage
            });
        }

        if (!string.IsNullOrEmpty(alert.Details))
        {
            sections.Add(new
            {
                title = "Details",
                text = alert.Details
            });
        }

        var payload = new
        {
            type = "MessageCard",
            context = "https://schema.org/extensions",
            themeColor,
            summary = alert.GetTitle(),
            sections
        };

        return JsonSerializer.Serialize(payload);
    }

    private string CreateGenericPayload(SecurityAlert alert)
    {
        var payload = new
        {
            eventId = alert.EventId,
            eventType = alert.EventType.ToString(),
            severity = alert.Severity.ToString(),
            timestamp = alert.Timestamp,
            isSuccessful = alert.IsSuccessful,
            userId = alert.UserId,
            username = alert.Username,
            ipAddress = alert.IpAddress,
            userAgent = alert.UserAgent,
            errorMessage = alert.ErrorMessage,
            details = alert.Details
        };

        return JsonSerializer.Serialize(payload);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            clone.Content = new StringContent(content, Encoding.UTF8, "application/json");
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
