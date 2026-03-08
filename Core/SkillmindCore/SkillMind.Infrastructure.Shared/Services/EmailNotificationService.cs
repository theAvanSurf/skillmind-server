using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;

namespace SkillMind.Infrastructure.Shared.Services;

/// <summary>
/// Delivers transactional emails for Stripe subscription lifecycle events via SMTP.
/// </summary>
public class EmailNotificationService(
    IOptions<EmailSettings> emailOptions,
    ILogger<EmailNotificationService> logger)
    : IEmailNotificationService
{
    private readonly EmailSettings _cfg = emailOptions.Value;

    /// <inheritdoc/>
    public Task SendPaymentFailedEmail(string toEmail, string userName, DateTime gracePeriodEnd, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            "Payment Failed — Action Required",
            $"""
            <p>Hi {userName},</p>
            <p>We were unable to process your payment.</p>
            <p>You have until <strong>{gracePeriodEnd:MMMM d, yyyy}</strong> to update your payment details before your subscription is suspended.</p>
            <p>Please log in and update your billing information to avoid any interruption.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    /// <inheritdoc/>
    public Task SendGracePeriodWarningEmail(string toEmail, string userName, int daysRemaining, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            "Grace Period Ending Soon",
            $"""
            <p>Hi {userName},</p>
            <p>Your grace period expires in <strong>{daysRemaining} day(s)</strong>.</p>
            <p>After this date your subscription will be suspended if payment remains outstanding.</p>
            <p>Please update your payment method now to keep uninterrupted access.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    /// <inheritdoc/>
    public Task SendPaymentRecoveredEmail(string toEmail, string userName, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            "Payment Successful — Subscription Restored",
            $"""
            <p>Hi {userName},</p>
            <p>Great news! Your payment was successfully processed.</p>
            <p>Your subscription is now fully active.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    /// <inheritdoc/>
    public Task SendSubscriptionSuspendedEmail(string toEmail, string userName, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            "Subscription Suspended",
            $"""
            <p>Hi {userName},</p>
            <p>Your subscription has been suspended because your grace period has ended without a successful payment.</p>
            <p>You can reactivate your subscription at any time by updating your payment details.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    /// <inheritdoc/>
    public Task SendTrialEndingEmail(string toEmail, string userName, DateTime trialEnd, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            "Your Free Trial Is Ending Soon",
            $"""
            <p>Hi {userName},</p>
            <p>Your free trial ends on <strong>{trialEnd:MMMM d, yyyy}</strong>.</p>
            <p>Add a payment method now to continue without interruption after your trial expires.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    /// <inheritdoc/>
    public Task SendSubscriptionCanceledEmail(string toEmail, string userName, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            "Subscription Canceled",
            $"""
            <p>Hi {userName},</p>
            <p>Your subscription has been canceled.</p>
            <p>You can resubscribe at any time — your learning history will be waiting.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    /// <inheritdoc/>
    public Task SendRetryAttemptEmail(string toEmail, string userName, int attemptNumber, DateTime nextRetry, CancellationToken ct = default)
        => SendAsync(
            toEmail,
            $"Payment Retry Attempt {attemptNumber}",
            $"""
            <p>Hi {userName},</p>
            <p>We automatically retried your payment (attempt {attemptNumber}) but it was unsuccessful.</p>
            <p>The next retry is scheduled for <strong>{nextRetry:MMMM d, yyyy h:mm tt} UTC</strong>.</p>
            <p>Please update your billing details to avoid suspension.</p>
            <p>— The SkillMind Team</p>
            """,
            ct);

    // ── Core SMTP send ────────────────────────────────────────────────────────

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            using var client = BuildSmtpClient();
            using var message = BuildMessage(toEmail, subject, htmlBody);
            await client.SendMailAsync(message, ct);
            logger.LogInformation("Email sent to {Recipient}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Email delivery failure must never crash the business logic.
            logger.LogError(ex, "Failed to send email to {Recipient}: {Subject}", toEmail, subject);
        }
    }

    private SmtpClient BuildSmtpClient() => new(_cfg.Host, _cfg.Port)
    {
        Credentials = new NetworkCredential(_cfg.Username, _cfg.Password),
        EnableSsl = _cfg.EnableSsl,
        DeliveryMethod = SmtpDeliveryMethod.Network,
    };

    private MailMessage BuildMessage(string toEmail, string subject, string htmlBody) => new()
    {
        From = new MailAddress(_cfg.From, _cfg.FromDisplayName),
        To = { toEmail },
        Subject = subject,
        Body = htmlBody,
        IsBodyHtml = true,
    };
}
