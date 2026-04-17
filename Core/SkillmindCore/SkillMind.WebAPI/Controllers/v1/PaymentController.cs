using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Stripe;
using SkillMind.Core.Application.Dtos.Courses;
using SkillMind.Core.Application.Dtos.Stripe;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Infrastructure.Shared.Services;

namespace SkillMind.WebAPI.Controllers.v1;

public class PaymentController(StripeServices stripeServices, ICourseService courseService) : BaseController
{
    private string ResolveOrigin()
    {
        var requestOrigin = Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(requestOrigin))
            return requestOrigin;

        var configuredOrigin = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(configuredOrigin))
            return configuredOrigin;

        return "http://localhost:5173";
    }

    private string ResolveLookupKey(string? lookupKey)
    {
        if (!string.IsNullOrWhiteSpace(lookupKey))
            return lookupKey;

        var configuredKey = Environment.GetEnvironmentVariable("PAYMENT_DEFAULT_LOOKUP_KEY");
        if (!string.IsNullOrWhiteSpace(configuredKey))
            return configuredKey;

        return "Skillmind_Premium_Plan-42a1204";
    }

    [Authorize]
    [HttpPost("create-checkout-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request body.");

        var lookupKey = ResolveLookupKey(dto.LookupKey);
        var origin = ResolveOrigin();

        try
        {
            var clientSecret = await stripeServices.CreateSession(lookupKey, origin);
            return Ok(new { clientSecret });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (NullReferenceException)
        {
            return BadRequest("Unable to create checkout session. Verify Stripe lookup key and Stripe configuration.");
        }
    }

    // Keep backward compatibility with gateway/frontend route naming.
    [Authorize]
    [HttpPost("create-subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateCheckoutSessionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest("Invalid request body.");

        var lookupKey = ResolveLookupKey(dto.LookupKey);
        var customerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(customerEmail))
            return Unauthorized("User email claim is required.");

        try
        {
            var result = await stripeServices.CreateSubscriptionPaymentIntent(lookupKey, customerEmail);
            return Ok(new { clientSecret = result.ClientSecret, subscriptionId = result.SubscriptionId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (StripeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("session-status")]
    [ProducesResponseType(typeof(SessionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SessionStatus([FromQuery] string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return BadRequest("sessionId is required.");

        var result = await stripeServices.GetSessionStatus(sessionId);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("create-portal-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePortalSession([FromBody] CreatePortalSessionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var origin = ResolveOrigin();
        string url;

        if (!string.IsNullOrWhiteSpace(dto.SessionId))
        {
            url = await stripeServices.CreatePortalSession(dto.SessionId, origin);
        }
        else
        {
            var customerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(customerEmail))
                return Unauthorized("User email claim is required.");

            try
            {
                url = await stripeServices.CreatePortalSessionByCustomerEmail(customerEmail, origin);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        return Ok(new { url });
    }

    [Authorize]
    [HttpGet("subscription")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSubscription()
    {
        var customerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(customerEmail) || string.IsNullOrWhiteSpace(userId))
            return Unauthorized("User identity claims are required.");

        var subscription = await stripeServices.GetSubscription(customerEmail, userId);
        if (subscription is null)
            return NotFound(new { message = "No subscription found" });

        return Ok(subscription);
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook()
    {
        var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(stripeSignature))
            return BadRequest("Missing Stripe-Signature header.");

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload, stripeSignature,
                Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                    ?? throw new StripeException("Webhook secret not configured."),
                throwOnApiVersionMismatch: false);

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                if (intent is not null
                    && intent.Metadata.TryGetValue("courseId", out var courseIdStr)
                    && intent.Metadata.TryGetValue("studentProfileId", out var profileIdStr)
                    && intent.Metadata.TryGetValue("type", out var type)
                    && type == "course_purchase"
                    && Guid.TryParse(courseIdStr, out var courseId)
                    && Guid.TryParse(profileIdStr, out var profileId))
                {
                    await courseService.ConfirmEnrollmentAsync(new ConfirmEnrollmentDto
                    {
                        PaymentIntentId = intent.Id,
                        CourseId = courseId,
                        StudentProfileId = profileId,
                        PaidAmount = intent.Amount / 100m
                    });
                }
            }

            // Pass to legacy subscription handler as well
            await stripeServices.HandleWebhook(payload, stripeSignature);
        }
        catch (StripeException ex)
        {
            Console.WriteLine("Webhook error: {0}", ex.Message);
            return BadRequest();
        }

        return Ok();
    }
}