namespace Aegis.Modules.Security.Application.Alerting;

/// <summary>
/// Configuration options for security alerting
/// </summary>
public sealed class AlertingOptions
{
    public const string SectionName = "Security:Alerting";

    public bool Enabled { get; set; } = false;

    public EmailOptions? Email { get; set; }
    public List<WebhookOptions> Webhooks { get; set; } = new();

    public sealed class EmailOptions
    {
        public bool Enabled { get; set; } = false;
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Aegis Messenger Security";
        public List<string> ToAddresses { get; set; } = new();
    }

    public sealed class WebhookOptions
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public WebhookType Type { get; set; } = WebhookType.Generic;
        public Dictionary<string, string> Headers { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 10;
        public int MaxRetries { get; set; } = 3;
    }

    public enum WebhookType
    {
        Generic,
        Slack,
        Discord,
        MicrosoftTeams
    }
}
