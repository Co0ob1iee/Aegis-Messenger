namespace Aegis.Desktop.Models;

public class DisappearingMessageTimer
{
    public string Label { get; set; } = string.Empty;
    public int? Seconds { get; set; }
    public bool IsDisabled => !Seconds.HasValue;

    public static DisappearingMessageTimer[] GetDefaultTimers() => new[]
    {
        new DisappearingMessageTimer { Label = "Off", Seconds = null },
        new DisappearingMessageTimer { Label = "30 seconds", Seconds = 30 },
        new DisappearingMessageTimer { Label = "1 minute", Seconds = 60 },
        new DisappearingMessageTimer { Label = "5 minutes", Seconds = 300 },
        new DisappearingMessageTimer { Label = "30 minutes", Seconds = 1800 },
        new DisappearingMessageTimer { Label = "1 hour", Seconds = 3600 },
        new DisappearingMessageTimer { Label = "1 day", Seconds = 86400 },
        new DisappearingMessageTimer { Label = "1 week", Seconds = 604800 },
        new DisappearingMessageTimer { Label = "4 weeks", Seconds = 2419200 }
    };
}
