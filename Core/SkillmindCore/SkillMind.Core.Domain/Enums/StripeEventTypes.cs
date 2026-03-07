namespace SkillMind.Core.Domain.Enums;

public enum StripeEventType
{
    CustomerSubscriptionDeleted,
    CustomerSubscriptionUpdated,
    CustomerSubscriptionCreated,
    CustomerSubscriptionTrialWillEnd,
    ActiveEntitlementSummaryUpdated,
    Unknown
}