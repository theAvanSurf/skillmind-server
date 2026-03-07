namespace SkillMind.Core.Domain.Settings;

public class StripeConfigurations
{
    public required string SecretKey { get; set; }
    public required string PublishableKey { get; set; }
    public required string WebhookSecret { get; set; }
}