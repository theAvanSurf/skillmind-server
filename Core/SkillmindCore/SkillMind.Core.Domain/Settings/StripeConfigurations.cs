namespace SkillMind.Core.Domain.Settings;

public class StripeConfigurations
{
    public required string SecretKey { get; set; }
    public required string PublishableKey { get; set; }
    public required string WebhookSecret { get; set; }
    /// <summary>Webhook signing secret for Stripe Connect account events (account.updated).</summary>
    public string ConnectWebhookSecret { get; set; } = string.Empty;
    /// <summary>Platform fee percentage taken from each course purchase (0.0–1.0). Default 10%.</summary>
    public decimal PlatformFeePercent { get; set; } = 0.10m;
}