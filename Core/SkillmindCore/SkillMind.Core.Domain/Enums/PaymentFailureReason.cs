namespace SkillMind.Core.Domain.Enums;

/// <summary>Describes why a Stripe payment failed.</summary>
public enum PaymentFailureReason
{
    /// <summary>Card was declined due to insufficient funds.</summary>
    InsufficientFunds,

    /// <summary>Card was generically declined by the issuer.</summary>
    CardDeclined,

    /// <summary>A network-level error occurred during the charge attempt.</summary>
    NetworkError,

    /// <summary>Bank processing delay prevented the charge from completing.</summary>
    BankDelay,

    /// <summary>Card is expired.</summary>
    Expired,

    /// <summary>Reason could not be determined.</summary>
    Unknown
}
