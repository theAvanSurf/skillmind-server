using SkillMind.Core.Domain.Enums;
using Stripe;

namespace SkillMind.Infrastructure.Shared.Helpers;

public static class StripeEventMapper
{
    public static StripeEventType Map(string eventType) => eventType switch
    {
        EventTypes.CustomerSubscriptionDeleted              => StripeEventType.CustomerSubscriptionDeleted,
        EventTypes.CustomerSubscriptionUpdated              => StripeEventType.CustomerSubscriptionUpdated,
        EventTypes.CustomerSubscriptionCreated              => StripeEventType.CustomerSubscriptionCreated,
        EventTypes.CustomerSubscriptionTrialWillEnd         => StripeEventType.CustomerSubscriptionTrialWillEnd,
        EventTypes.EntitlementsActiveEntitlementSummaryUpdated => StripeEventType.ActiveEntitlementSummaryUpdated,
        "invoice.payment_failed"                            => StripeEventType.InvoicePaymentFailed,
        "invoice.payment_succeeded"                         => StripeEventType.InvoicePaymentSucceeded,
        _                                                   => StripeEventType.Unknown
    };

    /// <summary>
    /// Maps a Stripe decline code to the typed <see cref="PaymentFailureReason"/>.
    /// </summary>
    public static PaymentFailureReason MapDeclineCode(string? declineCode) => declineCode switch
    {
        "insufficient_funds"           => PaymentFailureReason.InsufficientFunds,
        "card_declined"                => PaymentFailureReason.CardDeclined,
        "expired_card"                 => PaymentFailureReason.Expired,
        "do_not_honor"                 => PaymentFailureReason.CardDeclined,
        "processing_error"             => PaymentFailureReason.NetworkError,
        "bank_cannot_process"          => PaymentFailureReason.BankDelay,
        _                              => PaymentFailureReason.Unknown
    };
}
