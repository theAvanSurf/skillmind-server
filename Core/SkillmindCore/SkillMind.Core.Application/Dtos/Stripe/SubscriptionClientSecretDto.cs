namespace SkillMind.Core.Application.Dtos.Stripe;

/// <summary>Returned when a Stripe Subscription is created via the Elements flow.</summary>
public class SubscriptionClientSecretDto
{
    /// <summary>PaymentIntent client secret — pass to stripe.confirmPayment() on the frontend.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The Stripe Subscription ID.</summary>
    public string SubscriptionId { get; set; } = string.Empty;
}
