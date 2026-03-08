namespace SkillMind.Core.Application.Interfaces;

/// <summary>Sends transactional emails related to subscription lifecycle events.</summary>
public interface IEmailNotificationService
{
    /// <summary>Notifies the user that their payment failed and a grace period has started.</summary>
    Task SendPaymentFailedEmail(string email, string userName, DateTime gracePeriodEnd, CancellationToken ct = default);

    /// <summary>Reminds the user how many days remain in their grace period.</summary>
    Task SendGracePeriodWarningEmail(string email, string userName, int daysRemaining, CancellationToken ct = default);

    /// <summary>Congratulates the user that their payment was recovered successfully.</summary>
    Task SendPaymentRecoveredEmail(string email, string userName, CancellationToken ct = default);

    /// <summary>Informs the user that their subscription has been suspended due to non-payment.</summary>
    Task SendSubscriptionSuspendedEmail(string email, string userName, CancellationToken ct = default);

    /// <summary>Alerts the user that their trial period is ending soon.</summary>
    Task SendTrialEndingEmail(string email, string userName, DateTime trialEnd, CancellationToken ct = default);

    /// <summary>Confirms that the user's subscription has been canceled.</summary>
    Task SendSubscriptionCanceledEmail(string email, string userName, CancellationToken ct = default);

    /// <summary>Informs the user that a retry attempt is scheduled.</summary>
    Task SendRetryAttemptEmail(string email, string userName, int attemptNumber, DateTime nextRetry, CancellationToken ct = default);
}
