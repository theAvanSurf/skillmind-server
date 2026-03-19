using SkillMind.Core.Application.Dtos.Stripe;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Shared.Helpers;
using Stripe;
using Stripe.Checkout;
using Stripe.Entitlements;

namespace SkillMind.Infrastructure.Shared.Services;

public class StripeServices(StripeConfigurations configurations, PriceService service, SessionService sessionService)
{
    private readonly PriceService _priceService = service;
    private readonly StripeConfigurations _stripeConfigurations = configurations;
    private readonly SessionService _sessionService = sessionService;

    public async Task<string> CreateSession(string lookupKey, string origin)
    {
        var priceOptions = new PriceListOptions
        {
            LookupKeys = { lookupKey }
        };

        StripeList<Price> prices = await _priceService.ListAsync(priceOptions);

        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = prices.Data[0].Id,
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