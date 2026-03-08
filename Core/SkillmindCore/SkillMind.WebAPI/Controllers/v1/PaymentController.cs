using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillMind.Core.Application.Dtos.Stripe;
using SkillMind.Core.Application.Interfaces;

namespace SkillMind.WebAPI.Controllers.v1;

[Authorize]
public class PaymentController(IStripeServices stripeServices) : BaseController
{
    /// <summary>Creates a Stripe Subscription (Elements flow) and returns the PaymentIntent client secret.</summary>
    [HttpPost("create-subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateCheckoutSessionDto dto,
        CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Guid.TryParse(userIdStr, out var userId);
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var result = await stripeServices.CreateSubscription(dto.LookupKey, userId, userEmail, ct);
        return Ok(result);
    }

    /// <summary>Creates a Stripe Embedded Checkout session and returns the client secret.</summary>
    [HttpPost("create-checkout-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionDto dto,
        CancellationToken ct)
    {
        var origin = HttpContext.Request.Headers.Origin.FirstOrDefault()
                     ?? HttpContext.Request.Headers["Referer"].FirstOrDefault()
                     ?? string.Empty;

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Guid.TryParse(userIdStr, out var userId);

        var clientSecret = await stripeServices.CreateSession(dto.LookupKey, origin, userId, ct);
        return Ok(new { clientSecret });
    }

    /// <summary>Returns the Checkout Session status and customer email.</summary>
    [HttpGet("session-status")]
    [ProducesResponseType(typeof(SessionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessionStatus(
        [FromQuery] string sessionId,
        CancellationToken ct)
    {
        var result = await stripeServices.GetSessionStatus(sessionId, ct);
        return Ok(result);
    }

    /// <summary>Creates a Stripe Billing Portal session and returns the redirect URL.</summary>
    [HttpPost("create-portal-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePortalSession(
        [FromBody] CreatePortalSessionDto dto,
        CancellationToken ct)
    {
        var origin = HttpContext.Request.Headers.Origin.FirstOrDefault()
                     ?? HttpContext.Request.Headers["Referer"].FirstOrDefault()
                     ?? string.Empty;

        var url = await stripeServices.CreatePortalSession(dto.SessionId, origin, ct);
        return Ok(new { url });
    }

    /// <summary>
    /// Stripe webhook endpoint. Does NOT require authentication — Stripe cannot send a JWT.
    /// Signature verification is handled inside <see cref="IStripeServices.HandleWebhook"/>.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        // Stripe requires the raw body for signature verification.
        // Disable buffering so we can read the raw stream.
        using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync(ct);

        var signature = HttpContext.Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest("Missing Stripe-Signature header.");

        var handled = await stripeServices.HandleWebhook(json, signature, ct);
        return handled ? Ok() : BadRequest("Invalid Stripe signature.");
    }

    /// <summary>Returns the current Stripe subscription for the authenticated user.</summary>
    [HttpGet("subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(
        [FromServices] ISubscriptionRepository subscriptionRepo,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User identity could not be determined.");

        var subscription = await subscriptionRepo.GetByUserId(userId, ct);
        if (subscription is null) return NotFound("No subscription found for this user.");

        return Ok(subscription);
    }
}
