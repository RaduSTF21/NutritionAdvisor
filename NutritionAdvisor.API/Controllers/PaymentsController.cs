using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using System.Linq;
using System.Security.Claims;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public PaymentsController(IPaymentService paymentService, IUserRepository userRepository, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _userRepository = userRepository;
        _configuration = configuration;
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
            Console.WriteLine($"[Stripe Cancel Error] {e.Message}");
            return StatusCode(500, "Nu am putut anula abonamentul în Stripe.");
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret, throwOnApiVersionMismatch: false);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session?.Metadata != null && session.Metadata.TryGetValue("userId", out var userId))
                {
                    var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                    if (user != null)
                    {
                        user.SubscriptionPlan = SubscriptionPlan.Premium;
                        user.SubscriptionStatus = SubscriptionStatus.Active;
                        user.ProviderSubscriptionId = session.SubscriptionId;
                        user.AutoRenew = true;

                        // --- ADĂUGARE NOUĂ ---
                        // Interogăm Stripe pentru a lua detaliile abonamentului nou creat (pentru prima lună)
                        try
                        {
                            var subService = new SubscriptionService();
                            var stripeSub = await subService.GetAsync(session.SubscriptionId);
                            user.SubscriptionEndAt = stripeSub.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STRIPE WARNING] Nu am putut lua data abonamentului inițial: {ex.Message}");
                        }
                        // ---------------------

                        await _userRepository.UpdateAsync(user);
                        Console.WriteLine($"[STRIPE] Sesiune finalizată. Abonament generat: {session.SubscriptionId}. Expiră la: {user.SubscriptionEndAt}");
                    }
                }
            }
            else if (stripeEvent.Type == "customer.subscription.updated")
            {
                var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;

                if (stripeSubscription != null)
                {
                    var user = await _userRepository.GetByProviderSubscriptionIdAsync(stripeSubscription.Id);

                    if (user != null)
                    {
                        user.SubscriptionEndAt = stripeSubscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
                        user.AutoRenew = !stripeSubscription.CancelAtPeriodEnd;

                        await _userRepository.UpdateAsync(user);
                        Console.WriteLine($"[STRIPE] Abonament actualizat. Noua expirare: {user.SubscriptionEndAt}. Auto-renew: {user.AutoRenew}");
                    }
                }
            }
            else if (stripeEvent.Type == "customer.subscription.deleted")
            {
                var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;

                if (stripeSubscription != null)
                {
                    var user = await _userRepository.GetByProviderSubscriptionIdAsync(stripeSubscription.Id);

                    if (user != null)
                    {
                        user.SubscriptionPlan = SubscriptionPlan.Free;
                        user.SubscriptionStatus = SubscriptionStatus.Inactive;
                        user.ProviderSubscriptionId = null;
                        user.AutoRenew = false;

                        await _userRepository.UpdateAsync(user);
                        Console.WriteLine($"[STRIPE] Abonament expirat definitiv pentru {user.Email}. Downgrade la Free.");
                    }
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            Console.WriteLine($"[STRIPE ERROR] Validare eșuată: {e.Message}");
            return BadRequest();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WEBHOOK ERROR] Eroare generală: {e.Message}");
            return StatusCode(500);
        }
    }
}