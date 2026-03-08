namespace SkillMind.Core.Application.Dtos.Stripe;

/// <summary>Request body for creating a new Stripe Embedded Checkout session.</summary>
public record CreateCheckoutSessionDto(string LookupKey);
