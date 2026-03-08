namespace SkillMind.Core.Domain.Settings;

public class StripeConfigurations
{
    public const string SectionName = "StripeConfigurations";

    public required string SecretKey { get; set; }
    public required string PublishableKey { get; set; }
    public required string WebhookSecret { get; set; }

    /// <summary>Base URL of the frontend — used as the Stripe ReturnUrl origin (e.g. http://localhost:3005).</summary>
    public string FrontendUrl { get; set; } = string.Empty;

    /// <summary>Days before a PastDue subscription is suspended. Default: 3.</summary>
    public int GracePeriodDays { get; set; } = 3;

    /// <summary>Hours between automatic payment retry attempts. Default: 8.</summary>
    public int RetryIntervalHours { get; set; } = 8;

    /// <summary>Maximum number of automatic retry attempts before giving up. Default: 9.</summary>
    public int MaxRetryAttempts { get; set; } = 9;
}