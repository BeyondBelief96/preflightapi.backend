using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PreflightApi.API.Models;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Settings;
using Stripe;

namespace PreflightApi.API.Controllers;

/// <summary>
/// Receives Stripe webhook events for subscription lifecycle management.
/// Verified via Stripe webhook signature — no API key or JWT auth required.
/// </summary>
[ApiController]
[Route("webhooks/stripe")]
[Tags("Webhooks")]
public class StripeWebhookController(
    IStripeWebhookService webhookService,
    IOptions<StripeSettings> stripeSettings,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Handle incoming Stripe webhook events. Verified by Stripe-Signature header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            return BadRequest(new ApiErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "Missing Stripe-Signature header.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, signature, stripeSettings.Value.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return BadRequest(new ApiErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "Invalid Stripe webhook signature.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        await webhookService.ProcessEventAsync(stripeEvent, ct);

        return Ok();
    }
}
