namespace SkillMind.Core.Application.Dtos.Stripe;

public class SubscriptionDto
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string StripeSubscriptionId { get; set; }
    public required string Plan { get; set; }
    public required string SubscriptionStatus { get; set; }
    public required string CurrentPeriodEnd { get; set; }
    public bool IsInGracePeriod { get; set; }
    public string? GracePeriodEnd { get; set; }
    public string? IntendedPlan { get; set; }
}
