using SkillMind.Core.Application.Dtos.Stripe;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Stripe;
using Stripe.Checkout;
using Stripe.Entitlements;

namespace SkillMind.Infrastructure.Shared.Services;

public class StripeServices(IOptions<StripeConfigurations> configurations, PriceService service, SessionService sessionService)
{
    private readonly PriceService _priceService = service;
    private readonly StripeConfigurations _stripeConfigurations = configurations.Value;
    private readonly SessionService _sessionService = sessionService;

    private async Task<Price> ResolvePriceByLookupKey(string lookupKey)
    {
        var priceOptions = new PriceListOptions
        {
            LookupKeys = new List<string> { lookupKey }
        };

        StripeList<Price>? prices = await _priceService.ListAsync(priceOptions);
        var selectedPrice = prices?.Data?.FirstOrDefault();
        if (selectedPrice is null || string.IsNullOrWhiteSpace(selectedPrice.Id))
            throw new InvalidOperationException($"No Stripe price found for lookup key '{lookupKey}'.");

        return selectedPrice;
    }

    private static string? ExtractClientSecretFromInvoiceJson(string? invoiceJson)
    {
        if (string.IsNullOrWhiteSpace(invoiceJson))
            return null;

        using var document = JsonDocument.Parse(invoiceJson);
        var invoiceRoot = document.RootElement;

        // Preferred path in latest Stripe API shapes.
        if (invoiceRoot.TryGetProperty("confirmation_secret", out var confirmationSecret) &&
            confirmationSecret.TryGetProperty("client_secret", out var confirmationClientSecret) &&
            confirmationClientSecret.ValueKind == JsonValueKind.String)
        {
            return confirmationClientSecret.GetString();
        }

        // Backward-compatible path for expanded payment_intent.
        if (invoiceRoot.TryGetProperty("payment_intent", out var paymentIntent) &&
            paymentIntent.ValueKind == JsonValueKind.Object &&
            paymentIntent.TryGetProperty("client_secret", out var paymentIntentClientSecret) &&
            paymentIntentClientSecret.ValueKind == JsonValueKind.String)
        {
            return paymentIntentClientSecret.GetString();
        }

        return null;
    }

    public async Task<string> CreateSession(string lookupKey, string origin)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
            throw new ArgumentException("lookupKey is required.", nameof(lookupKey));

        if (string.IsNullOrWhiteSpace(origin))
            throw new ArgumentException("origin is required.", nameof(origin));

        var selectedPrice = await ResolvePriceByLookupKey(lookupKey);

        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = selectedPrice.Id,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            UiMode = "embedded",
            ReturnUrl = origin + "/return?session_id={CHECKOUT_SESSION_ID}",
        };

        var session = await _sessionService.CreateAsync(options);
        return session.ClientSecret;
    }

    public async Task<(string ClientSecret, string SubscriptionId)> CreateSubscriptionPaymentIntent(string lookupKey, string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
            throw new ArgumentException("lookupKey is required.", nameof(lookupKey));

        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("customerEmail is required.", nameof(customerEmail));

        var selectedPrice = await ResolvePriceByLookupKey(lookupKey);

        var customerService = new CustomerService();
        var customers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = customerEmail,
            Limit = 1,
        });

        var customer = customers.Data?.FirstOrDefault();
        if (customer is null)
        {
            customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = customerEmail,
            });
        }

        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customer.Id,
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Price = selectedPrice.Id,
                },
            },
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription",
            },
            Expand = new List<string>
            {
                "latest_invoice.payment_intent",
                "latest_invoice.confirmation_secret",
            },
        });

        var invoiceId = subscription.LatestInvoiceId;
        if (string.IsNullOrWhiteSpace(invoiceId))
            throw new InvalidOperationException("Stripe did not return a latest invoice for this subscription.");

        var invoiceService = new InvoiceService();
        var invoice = await invoiceService.GetAsync(invoiceId, new InvoiceGetOptions
        {
            Expand = new List<string>
            {
                "payment_intent",
                "confirmation_secret",
            },
        });

        var clientSecret = ExtractClientSecretFromInvoiceJson(invoice.StripeResponse?.Content);

        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("Stripe did not return a payment intent client secret for this subscription.");

        return (clientSecret, subscription.Id);
    }

    public async Task<SessionStatusDto> GetSessionStatus(string sessionId)
    {
        var session = await _sessionService.GetAsync(sessionId);
        return new SessionStatusDto
        {
            Status = session.Status,
            CustomerEmail = session.CustomerDetails.Email
        };
    }

    public async Task<string> CreatePortalSession(string sessionId, string origin)
    {
        var checkoutSession = await _sessionService.GetAsync(sessionId);

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = checkoutSession.CustomerId,
            ReturnUrl = origin,
        };

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(options);
        return session.Url;
    }

    public Task<bool> HandleWebhook(string json, string stripeSignature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _stripeConfigurations.WebhookSecret
            );

            switch (StripeEventMapper.Map(stripeEvent.Type))
            {
                case StripeEventType.CustomerSubscriptionCreated:
                    var createdSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.CustomerSubscriptionUpdated:
                    var updatedSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.CustomerSubscriptionDeleted:
                    var deletedSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.CustomerSubscriptionTrialWillEnd:
                    var trialSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.ActiveEntitlementSummaryUpdated:
                    var summary = stripeEvent.Data.Object as ActiveEntitlementSummary;
                    break;

                case StripeEventType.Unknown:
                default:
                    Console.WriteLine("Unhandled Stripe event: {0}", stripeEvent.Type);
                    break;
            }

            return Task.FromResult(true);
        }
        catch (StripeException e)
        {
            Console.WriteLine("Stripe webhook error: {0}", e.Message);
            return Task.FromResult(false);
        }
    }
}