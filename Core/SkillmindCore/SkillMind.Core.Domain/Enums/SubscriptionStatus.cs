namespace SkillMind.Core.Domain.Enums;

/// <summary>Lifecycle state of a user's subscription.</summary>
public enum SubscriptionStatus
{
    /// <summary>Subscription is active and in good standing.</summary>
    Active,

    /// <summary>Payment failed; grace period is active.</summary>
    PastDue,

    /// <summary>Grace period expired without successful payment; access blocked.</summary>
    Suspended,

    /// <summary>Subscription was explicitly canceled by the user.</summary>
    Canceled,

    /// <summary>Subscription was deleted (e.g., after suspension and no recovery).</summary>
    Deleted,

    /// <summary>Subscription is in a trial period.</summary>
    Trialing,

    /// <summary>User is on the free tier (no Stripe subscription).</summary>
    Free
}
