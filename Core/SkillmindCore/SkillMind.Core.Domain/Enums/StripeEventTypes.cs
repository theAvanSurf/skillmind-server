namespace SkillMind.Core.Domain.Enums;

public enum StripeEventType
{
    CustomerSubscriptionDeleted,
    CustomerSubscriptionUpdated,
    CustomerSubscriptionCreated,
    CustomerSubscriptionTrialWillEnd,
    ActiveEntitlementSummaryUpdated,

    // Invoice events — used for payment failure/recovery flow
    InvoicePaymentFailed,
    InvoicePaymentSucceeded,

    Unknown
}