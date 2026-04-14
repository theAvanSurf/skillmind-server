using SkillMind.Core.Domain.Enums;
using Stripe;

namespace SkillMind.Infrastructure.Shared.Helpers;

public static class StripeEventMapper
{
    public static StripeEventType Map(string eventType) => eventType switch
    {
        EventTypes.CustomerSubscriptionDeleted      => StripeEventType.CustomerSubscriptionDeleted,
        EventTypes.CustomerSubscriptionUpdated      => StripeEventType.CustomerSubscriptionUpdated,
        EventTypes.CustomerSubscriptionCreated      => StripeEventType.CustomerSubscriptionCreated,
        EventTypes.CustomerSubscriptionTrialWillEnd => StripeEventType.CustomerSubscriptionTrialWillEnd,
        EventTypes.EntitlementsActiveEntitlementSummaryUpdated  => StripeEventType.ActiveEntitlementSummaryUpdated,
        "payment_intent.succeeded"                  => StripeEventType.PaymentIntentSucceeded,
        "account.updated"                           => StripeEventType.AccountUpdated,
        _                                           => StripeEventType.Unknown
    };
}