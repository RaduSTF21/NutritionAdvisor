using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IUserRepository _userRepository;

    public PaymentsController(IPaymentService paymentService, IUserRepository userRepository)
    {
        _paymentService = paymentService;
        _userRepository = userRepository;
    }

    [Authorize]
    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var url = await _paymentService.CreateCheckoutSessionAsync(userEmail!, userId);
        return Ok(new { Url = url });
    }
}

[ApiController]
[Route("api/webhook/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public StripeWebhookController(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromHeader(Name = "Stripe-Signature")] string signature)
    {
        // Stripe necesită corpul cererii ca string raw pentru verificarea semnăturii. Model Binding-ul modifică corpul cererii și strică semnătura.
#pragma warning disable S6932 
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
#pragma warning restore S6932

        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret, throwOnApiVersionMismatch: false);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;
            }

            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session || session.Metadata == null || !session.Metadata.TryGetValue("userId", out var userId))
            return;

        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
        if (user == null) return;

        user.SubscriptionPlan = SubscriptionPlan.Premium;
        user.SubscriptionStatus = SubscriptionStatus.Active;
        user.ProviderSubscriptionId = session.SubscriptionId;
        user.AutoRenew = true;

        try
        {
            var subService = new SubscriptionService();
            var stripeSub = await subService.GetAsync(session.SubscriptionId);
            user.SubscriptionEndAt = stripeSub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        }
        catch { /* ignored */ }

        await _userRepository.UpdateAsync(user);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSubscription) return;

        var user = await _userRepository.GetByProviderSubscriptionIdAsync(stripeSubscription.Id);
        if (user == null) return;

        user.SubscriptionEndAt = stripeSubscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        user.AutoRenew = !stripeSubscription.CancelAtPeriodEnd;
        await _userRepository.UpdateAsync(user);
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Stripe.Subscription stripeSubscription) return;

        var user = await _userRepository.GetByProviderSubscriptionIdAsync(stripeSubscription.Id);
        if (user == null) return;

        user.SubscriptionPlan = SubscriptionPlan.Free;
        user.SubscriptionStatus = SubscriptionStatus.Inactive;
        user.ProviderSubscriptionId = null;
        user.AutoRenew = false;
        await _userRepository.UpdateAsync(user);
    }
}

[ApiController]
[Route("api/payments")]
public class SubscriptionsController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public SubscriptionsController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [Authorize]
    [HttpPost("cancel-subscription")]
    public async Task<IActionResult> CancelSubscription()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.ProviderSubscriptionId))
            return BadRequest("Nu a fost găsit un abonament activ.");

        try
        {
            var service = new SubscriptionService();
            var cancelOptions = new SubscriptionUpdateOptions { CancelAtPeriodEnd = true };
            await service.UpdateAsync(user.ProviderSubscriptionId, cancelOptions);

            return Ok();
        }
        catch (StripeException e)
        {
            return StatusCode(500, $"Stripe Cancel Error: {e.Message}");
        }
    }
}