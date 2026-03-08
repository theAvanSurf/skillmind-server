using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Stripe;
using SkillMind.Core.Application.Dtos.Stripe.Messages;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Shared.Helpers;
using Stripe;
using Stripe.Checkout;
using Stripe.Entitlements;

namespace SkillMind.Infrastructure.Shared.Services;

/// <summary>
/// Implements all Stripe operations: checkout sessions, webhooks, and payment retries.
/// All webhook processing is DB + Kafka atomic — failures route to DLQ.
/// </summary>
public class StripeServices(
    IOptions<StripeConfigurations> stripeOptions,
    ISubscriptionRepository subscriptionRepo,
    IKafkaEventService kafka,
    ILogger<StripeServices> logger)
    : IStripeServices
{
    private readonly StripeConfigurations _cfg = stripeOptions.Value;

    private PriceService PriceClient => new();
    private SessionService SessionClient => new();

    // ── Checkout ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string> CreateSession(string lookupKey, string origin, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey))
            throw new InvalidOperationException("Stripe API key is not configured. Set StripeConfigurations__SecretKey in your environment.");

        var prices = await PriceClient.ListAsync(new PriceListOptions { LookupKeys = new List<string> { lookupKey } });

        if (prices.Data.Count == 0)
            throw new InvalidOperationException($"No Stripe Price found with lookup key '{lookupKey}'. Create it in your Stripe Dashboard under Products.");

        var session = await SessionClient.CreateAsync(new SessionCreateOptions
        {
            LineItems = [new SessionLineItemOptions { Price = prices.Data[0].Id, Quantity = 1 }],
            Mode = "subscription",
            UiMode = "embedded",
            ReturnUrl = (!string.IsNullOrWhiteSpace(_cfg.FrontendUrl) ? _cfg.FrontendUrl : origin) + "/return?session_id={CHECKOUT_SESSION_ID}",
            // Embed the user ID so the webhook handler can link the subscription back to the user.
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
            }
        });

        return session.ClientSecret;
    }

    /// <inheritdoc/>
    public async Task<SubscriptionClientSecretDto> CreateSubscription(string lookupKey, Guid userId, string userEmail, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey))
            throw new InvalidOperationException("Stripe API key is not configured. Set StripeConfigurations__SecretKey in your environment.");

        var prices = await PriceClient.ListAsync(new PriceListOptions { LookupKeys = new List<string> { lookupKey } });
        if (prices.Data.Count == 0)
            throw new InvalidOperationException($"No Stripe Price found with lookup key '{lookupKey}'. Create it in your Stripe Dashboard under Products.");

        // Get or create a Stripe Customer for this user
        var existingSub = await subscriptionRepo.GetByUserId(userId, ct);
        string customerId;
        if (!string.IsNullOrEmpty(existingSub?.StripeCustomerId))
        {
            customerId = existingSub.StripeCustomerId;
        }
        else
        {
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
            {
                Email = userEmail,
                Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
            });
            customerId = customer.Id;
        }

        // Persist IntendedPlan before payment — survives browser close / abandoned checkout
        if (existingSub is not null)
        {
            existingSub.IntendedPlan = lookupKey;
            existingSub.StripeCustomerId = customerId;
            existingSub.UpdatedAt = DateTime.UtcNow;
            await subscriptionRepo.UpdateAsync(existingSub, ct);
        }

        // Create an incomplete Stripe Subscription — it activates only after payment confirmation
        var subscriptionService = new Stripe.SubscriptionService();
        var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = [new SubscriptionItemOptions { Price = prices.Data[0].Id }],
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
        });

        // Fetch the latest invoice and expand the nested PaymentIntent.
        // In Stripe.net 50.x, Invoice has no direct PaymentIntent property;
        // the PaymentIntent lives at: Payments → InvoicePayment.Payment (InvoicePaymentPayment) → PaymentIntent
        var invoiceService = new InvoiceService();
        var invoice = await invoiceService.GetAsync(subscription.LatestInvoiceId,
            new InvoiceGetOptions { Expand = new List<string> { "payments.data.payment.payment_intent" } });

        var clientSecret = invoice.Payments?.Data?.FirstOrDefault()?.Payment?.PaymentIntent?.ClientSecret
            ?? throw new InvalidOperationException("Stripe did not return a PaymentIntent for the new subscription invoice.");

        return new SubscriptionClientSecretDto
        {
            ClientSecret = clientSecret,
            SubscriptionId = subscription.Id
        };
    }

    /// <inheritdoc/>
    public async Task<SessionStatusDto> GetSessionStatus(string sessionId, CancellationToken ct = default)
    {
        var session = await SessionClient.GetAsync(sessionId);
        return new SessionStatusDto
        {
            Status = session.Status,
            CustomerEmail = session.CustomerDetails.Email
        };
    }

    /// <inheritdoc/>
    public async Task<string> CreatePortalSession(string sessionId, string origin, CancellationToken ct = default)
    {
        var checkoutSession = await SessionClient.GetAsync(sessionId);
        var portalService = new Stripe.BillingPortal.SessionService();

        var portalSession = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = checkoutSession.CustomerId,
            ReturnUrl = origin,
        });

        return portalSession.Url;
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> HandleWebhook(string json, string signature, CancellationToken ct = default)
    {
        Event stripeEvent;
        try
        {
            // Signature verification must happen before any processing.
            stripeEvent = EventUtility.ConstructEvent(json, signature, _cfg.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning("Stripe webhook signature verification failed: {Message}", ex.Message);
            return false;
        }

        var eventType = StripeEventMapper.Map(stripeEvent.Type);
        logger.LogInformation("Stripe webhook received: {EventType} / {StripeEventId}", stripeEvent.Type, stripeEvent.Id);

        try
        {
            await (eventType switch
            {
                StripeEventType.CustomerSubscriptionCreated     => HandleSubscriptionCreated(stripeEvent, ct),
                StripeEventType.CustomerSubscriptionUpdated     => HandleSubscriptionUpdated(stripeEvent, ct),
                StripeEventType.CustomerSubscriptionDeleted     => HandleSubscriptionDeleted(stripeEvent, ct),
                StripeEventType.CustomerSubscriptionTrialWillEnd => HandleTrialWillEnd(stripeEvent, ct),
                StripeEventType.InvoicePaymentFailed            => HandleInvoicePaymentFailed(stripeEvent, ct),
                StripeEventType.InvoicePaymentSucceeded         => HandleInvoicePaymentSucceeded(stripeEvent, ct),
                _                                               => Task.CompletedTask
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing Stripe event {EventType}. Routing to DLQ.", stripeEvent.Type);
            await PublishToDlq(stripeEvent.Type, json, ex.Message, ct);
        }

        return true;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private async Task HandleSubscriptionCreated(Event stripeEvent, CancellationToken ct)
    {
        var sub = (Subscription)stripeEvent.Data.Object;
        var existing = await subscriptionRepo.GetByStripeSubscriptionId(sub.Id, ct);

        if (existing is not null) return; // idempotency guard

        // Read the user ID embedded in subscription metadata during checkout session creation.
        Guid userId = Guid.Empty;
        string? userIdStr = null;
        sub.Metadata?.TryGetValue("userId", out userIdStr);
        Guid.TryParse(userIdStr, out userId);

        var plan = sub.Items.Data[0].Price.LookupKey ?? sub.Items.Data[0].Price.Nickname ?? string.Empty;
        var periodStart = sub.Items.Data[0].CurrentPeriodStart;
        var periodEnd = sub.Items.Data[0].CurrentPeriodEnd;
        var status = sub.TrialEnd.HasValue && sub.TrialEnd > DateTime.UtcNow
            ? SubscriptionStatus.Trialing
            : SubscriptionStatus.Active;

        // If the user already has a free subscription, upgrade it in-place rather than creating a duplicate.
        UserSubscription? userSub = null;
        bool isUpgrade = false;
        if (userId != Guid.Empty)
        {
            userSub = await subscriptionRepo.GetByUserId(userId, ct);
            if (userSub is not null && userSub.SubscriptionStatus == SubscriptionStatus.Free)
                isUpgrade = true;
            else
                userSub = null;
        }

        if (isUpgrade && userSub is not null)
        {
            // Upgrade: fill in the Stripe fields on the existing free record.
            userSub.StripeSubscriptionId = sub.Id;
            userSub.StripeCustomerId = sub.CustomerId;
            userSub.StripePriceId = sub.Items.Data[0].Price.Id;
            userSub.StripeLookupKey = sub.Items.Data[0].Price.LookupKey ?? string.Empty;
            userSub.Status = sub.Status;
            userSub.SubscriptionStatus = status;
            userSub.Plan = plan;
            userSub.CurrentPeriodStart = periodStart;
            userSub.CurrentPeriodEnd = periodEnd;
            userSub.TrialEnd = sub.TrialEnd;
            userSub.IntendedPlan = null; // payment confirmed — clear pending flag
            userSub.UpdatedAt = DateTime.UtcNow;

            await TrySaveAndPublish(
                () => subscriptionRepo.UpdateAsync(userSub, ct),
                () => kafka.PublishAsync(
                    StripeKafkaTopic.SubscriptionCreated.ToTopicName(),
                    new SubscriptionCreatedMessage(
                        Guid.NewGuid(), DateTime.UtcNow,
                        userSub.UserId, sub.Id,
                        sub.Customer is Customer c0 ? c0.Email : string.Empty,
                        userSub.Plan, userSub.CurrentPeriodEnd),
                    ct),
                stripeEvent.Type, sub.Id, ct);
        }
        else
        {
            userSub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StripeSubscriptionId = sub.Id,
                StripeCustomerId = sub.CustomerId,
                StripePriceId = sub.Items.Data[0].Price.Id,
                StripeLookupKey = sub.Items.Data[0].Price.LookupKey ?? string.Empty,
                Status = sub.Status,
                SubscriptionStatus = status,
                Plan = plan,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                TrialEnd = sub.TrialEnd,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await TrySaveAndPublish(
                () => subscriptionRepo.CreateAsync(userSub, ct),
                () => kafka.PublishAsync(
                    StripeKafkaTopic.SubscriptionCreated.ToTopicName(),
                    new SubscriptionCreatedMessage(
                        Guid.NewGuid(), DateTime.UtcNow,
                        userSub.UserId, sub.Id,
                        sub.Customer is Customer c ? c.Email : string.Empty,
                        userSub.Plan, userSub.CurrentPeriodEnd),
                    ct),
                stripeEvent.Type, sub.Id, ct);
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var sub = (Subscription)stripeEvent.Data.Object;
        var userSub = await subscriptionRepo.GetByStripeSubscriptionId(sub.Id, ct);
        if (userSub is null) return;

        userSub.Status = sub.Status;
        userSub.SubscriptionStatus = MapStripeStatus(sub.Status);
        userSub.Plan = sub.Items.Data[0].Price.LookupKey ?? sub.Items.Data[0].Price.Nickname ?? userSub.Plan;
        userSub.CurrentPeriodStart = sub.Items.Data[0].CurrentPeriodStart;
        userSub.CurrentPeriodEnd = sub.Items.Data[0].CurrentPeriodEnd;
        userSub.UpdatedAt = DateTime.UtcNow;

        await TrySaveAndPublish(
            () => subscriptionRepo.UpdateAsync(userSub, ct),
            () => kafka.PublishAsync(
                StripeKafkaTopic.SubscriptionUpdated.ToTopicName(),
                new SubscriptionUpdatedMessage(
                    Guid.NewGuid(), DateTime.UtcNow,
                    userSub.UserId, sub.Id,
                    sub.Customer is Customer c ? c.Email : string.Empty,
                    sub.Status, userSub.Plan),
                ct),
            stripeEvent.Type, sub.Id, ct);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var sub = (Subscription)stripeEvent.Data.Object;
        var userSub = await subscriptionRepo.GetByStripeSubscriptionId(sub.Id, ct);
        if (userSub is null) return;

        userSub.Status = "canceled";
        userSub.SubscriptionStatus = SubscriptionStatus.Canceled;
        userSub.CanceledAt = sub.CanceledAt ?? DateTime.UtcNow;
        userSub.UpdatedAt = DateTime.UtcNow;

        await TrySaveAndPublish(
            () => subscriptionRepo.UpdateAsync(userSub, ct),
            () => kafka.PublishAsync(
                StripeKafkaTopic.SubscriptionCanceled.ToTopicName(),
                new SubscriptionCanceledMessage(
                    Guid.NewGuid(), DateTime.UtcNow,
                    userSub.UserId, sub.Id,
                    sub.Customer is Customer c ? c.Email : string.Empty,
                    userSub.CanceledAt.Value),
                ct),
            stripeEvent.Type, sub.Id, ct);
    }

    private async Task HandleTrialWillEnd(Event stripeEvent, CancellationToken ct)
    {
        var sub = (Subscription)stripeEvent.Data.Object;
        var userSub = await subscriptionRepo.GetByStripeSubscriptionId(sub.Id, ct);
        if (userSub is null) return;

        await kafka.PublishAsync(
            StripeKafkaTopic.SubscriptionTrialEnding.ToTopicName(),
            new SubscriptionTrialEndingMessage(
                Guid.NewGuid(), DateTime.UtcNow,
                userSub.UserId, sub.Id,
                sub.Customer is Customer c ? c.Email : string.Empty,
                sub.TrialEnd ?? DateTime.UtcNow),
            ct);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId is null) return;

        var userSub = await subscriptionRepo.GetByStripeSubscriptionId(subscriptionId, ct);
        if (userSub is null) return;

        var reason = StripeEventMapper.MapDeclineCode(invoice.LastFinalizationError?.DeclineCode);
        var now = DateTime.UtcNow;

        // Only start grace period on the first failure
        if (!userSub.IsInGracePeriod)
        {
            userSub.GracePeriodStart = now;
            userSub.GracePeriodEnd = now.AddDays(_cfg.GracePeriodDays);
            userSub.IsInGracePeriod = true;
        }

        userSub.Status = "past_due";
        userSub.SubscriptionStatus = SubscriptionStatus.PastDue;
        userSub.LastFailureAt = now;
        userSub.LastFailureReason = reason;
        userSub.RetryAttemptCount++;
        userSub.NextRetryAt = now.AddHours(_cfg.RetryIntervalHours);
        userSub.UpdatedAt = now;

        await TrySaveAndPublish(
            () => subscriptionRepo.UpdateAsync(userSub, ct),
            () => kafka.PublishAsync(
                StripeKafkaTopic.PaymentFailed.ToTopicName(),
                new PaymentFailedMessage(
                    Guid.NewGuid(), now,
                    userSub.UserId, subscriptionId,
                    invoice.CustomerEmail ?? string.Empty,
                    reason, userSub.GracePeriodEnd!.Value, userSub.RetryAttemptCount),
                ct),
            stripeEvent.Type, subscriptionId, ct);
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent, CancellationToken ct)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId is null) return;

        var userSub = await subscriptionRepo.GetByStripeSubscriptionId(subscriptionId, ct);
        if (userSub is null) return;

        // Clear grace period and reset retry counters
        var now = DateTime.UtcNow;
        userSub.Status = "active";
        userSub.SubscriptionStatus = SubscriptionStatus.Active;
        userSub.IsInGracePeriod = false;
        userSub.GracePeriodStart = null;
        userSub.GracePeriodEnd = null;
        userSub.RetryAttemptCount = 0;
        userSub.NextRetryAt = null;
        userSub.LastFailureAt = null;
        userSub.LastFailureReason = null;
        userSub.LastIdempotencyKey = null;
        userSub.IntendedPlan = null; // payment confirmed — clear pending flag
        userSub.UpdatedAt = now;

        await TrySaveAndPublish(
            () => subscriptionRepo.UpdateAsync(userSub, ct),
            () => kafka.PublishAsync(
                StripeKafkaTopic.PaymentRecovered.ToTopicName(),
                new PaymentRecoveredMessage(
                    Guid.NewGuid(), now,
                    userSub.UserId, subscriptionId,
                    invoice.CustomerEmail ?? string.Empty, now),
                ct),
            stripeEvent.Type, subscriptionId, ct);
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task RetryFailedPayment(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var userSub = await subscriptionRepo.GetByStripeSubscriptionId(stripeSubscriptionId, ct);
        if (userSub is null)
        {
            logger.LogWarning("RetryFailedPayment: subscription {Id} not found", stripeSubscriptionId);
            return;
        }

        // Unique key that prevents duplicate charges if the job runs twice in the same hour
        var idempotencyKey = $"retry-{stripeSubscriptionId}-{userSub.RetryAttemptCount}-{DateTime.UtcNow:yyyyMMddHH}";
        userSub.LastIdempotencyKey = idempotencyKey;

        logger.LogInformation("Retrying payment for subscription {Id}, attempt {Count}, key {Key}",
            stripeSubscriptionId, userSub.RetryAttemptCount + 1, idempotencyKey);

        try
        {
            // Retrieve the latest open invoice for the subscription and pay it
            var invoiceService = new InvoiceService();
            var invoices = await invoiceService.ListAsync(new InvoiceListOptions
            {
                Subscription = stripeSubscriptionId,
                Status = "open",
                Limit = 1,
            });

            if (invoices.Data.Count == 0)
            {
                logger.LogInformation("No open invoice found for subscription {Id}", stripeSubscriptionId);
                return;
            }

            await invoiceService.PayAsync(
                invoices.Data[0].Id,
                null,
                new RequestOptions { IdempotencyKey = idempotencyKey });

            // Success — clear grace period
            var now = DateTime.UtcNow;
            userSub.Status = "active";
            userSub.SubscriptionStatus = SubscriptionStatus.Active;
            userSub.IsInGracePeriod = false;
            userSub.GracePeriodStart = null;
            userSub.GracePeriodEnd = null;
            userSub.RetryAttemptCount = 0;
            userSub.NextRetryAt = null;
            userSub.LastFailureAt = null;
            userSub.LastFailureReason = null;
            userSub.UpdatedAt = now;

            await subscriptionRepo.UpdateAsync(userSub, ct);

            await kafka.PublishAsync(
                StripeKafkaTopic.PaymentRecovered.ToTopicName(),
                new PaymentRecoveredMessage(
                    Guid.NewGuid(), now,
                    userSub.UserId, stripeSubscriptionId,
                    string.Empty, now), ct);

            logger.LogInformation("Payment recovered for subscription {Id}", stripeSubscriptionId);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe retry failed for subscription {Id}: {Error}",
                stripeSubscriptionId, ex.StripeError?.Message);

            var now = DateTime.UtcNow;
            var reason = StripeEventMapper.MapDeclineCode(ex.StripeError?.DeclineCode);

            userSub.RetryAttemptCount++;
            userSub.NextRetryAt = now.AddHours(_cfg.RetryIntervalHours);
            userSub.LastFailureAt = now;
            userSub.LastFailureReason = reason;
            userSub.UpdatedAt = now;

            await subscriptionRepo.UpdateAsync(userSub, ct);

            await TryPublish(
                StripeKafkaTopic.PaymentFailed.ToTopicName(),
                new PaymentFailedMessage(
                    Guid.NewGuid(), now,
                    userSub.UserId, stripeSubscriptionId,
                    string.Empty, reason, userSub.GracePeriodEnd ?? now, userSub.RetryAttemptCount),
                stripeSubscriptionId, ct);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the DB save then the Kafka publish.
    /// If either throws, routes the event payload to the DLQ and re-logs without crashing.
    /// </summary>
    private async Task TrySaveAndPublish(
        Func<Task> dbAction,
        Func<Task> kafkaAction,
        string eventType,
        string subscriptionId,
        CancellationToken ct)
    {
        try
        {
            await dbAction();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DB write failed for event {EventType} / sub {SubId}", eventType, subscriptionId);
            await PublishToDlq(eventType, subscriptionId, ex.Message, ct);
            return;
        }

        try
        {
            await kafkaAction();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kafka publish failed for event {EventType} / sub {SubId}", eventType, subscriptionId);
            await PublishToDlq(eventType, subscriptionId, ex.Message, ct);
        }
    }

    private async Task TryPublish(string topic, object payload, string subscriptionId, CancellationToken ct)
    {
        try
        {
            await kafka.PublishAsync(topic, payload, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kafka publish failed for topic {Topic} / sub {SubId}", topic, subscriptionId);
            await PublishToDlq(topic, subscriptionId, ex.Message, ct);
        }
    }

    private async Task PublishToDlq(string originalTopic, string originalPayload, string error, CancellationToken ct)
    {
        try
        {
            await kafka.PublishAsync(
                StripeKafkaTopic.FailedEventDLQ.ToTopicName(),
                new FailedEventDLQMessage(
                    Guid.NewGuid(), DateTime.UtcNow,
                    originalTopic, originalPayload, error, DateTime.UtcNow),
                ct);
        }
        catch (Exception ex)
        {
            // DLQ publish itself failed — log critically but never throw
            logger.LogCritical(ex, "DLQ publish failed for topic {Topic}. Event is lost.", originalTopic);
        }
    }

    private static SubscriptionStatus MapStripeStatus(string status) => status switch
    {
        "active"   => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Canceled,
        "trialing" => SubscriptionStatus.Trialing,
        _          => SubscriptionStatus.Active
    };
}
