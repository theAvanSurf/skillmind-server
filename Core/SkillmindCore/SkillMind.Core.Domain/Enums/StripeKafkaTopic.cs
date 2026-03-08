namespace SkillMind.Core.Domain.Enums;

/// <summary>Kafka topic identifiers for all Stripe-related events.</summary>
public enum StripeKafkaTopic
{
    SubscriptionCreated,
    SubscriptionUpdated,
    SubscriptionCanceled,
    SubscriptionTrialEnding,
    PaymentFailed,
    PaymentRecovered,
    SubscriptionSuspended,

    /// <summary>Dead Letter Queue — receives events that failed processing.</summary>
    FailedEventDLQ
}

/// <summary>Maps <see cref="StripeKafkaTopic"/> values to their Kafka topic name strings.</summary>
public static class StripeKafkaTopicExtensions
{
    private static readonly Dictionary<StripeKafkaTopic, string> _topics = new()
    {
        [StripeKafkaTopic.SubscriptionCreated]     = "stripe.subscription.created",
        [StripeKafkaTopic.SubscriptionUpdated]     = "stripe.subscription.updated",
        [StripeKafkaTopic.SubscriptionCanceled]    = "stripe.subscription.canceled",
        [StripeKafkaTopic.SubscriptionTrialEnding] = "stripe.subscription.trial-ending",
        [StripeKafkaTopic.PaymentFailed]           = "stripe.payment.failed",
        [StripeKafkaTopic.PaymentRecovered]        = "stripe.payment.recovered",
        [StripeKafkaTopic.SubscriptionSuspended]   = "stripe.subscription.suspended",
        [StripeKafkaTopic.FailedEventDLQ]          = "stripe.events.failed",
    };

    /// <summary>Returns the Kafka topic name string for the given enum value.</summary>
    public static string ToTopicName(this StripeKafkaTopic topic) => _topics[topic];
}
