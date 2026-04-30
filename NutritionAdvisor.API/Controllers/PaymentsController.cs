using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Enums;
using Stripe;
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

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        // Stripe are nevoie de body-ul RAW. 
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        // Luăm secretul din configurație (care acum vine sigur din Docker-Compose)
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret, throwOnApiVersionMismatch: false);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                if (session?.Metadata != null && session.Metadata.TryGetValue("userId", out var userId))
                {
                    var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                    if (user != null)
                    {
                        user.SubscriptionPlan = SubscriptionPlan.Premium;
                        user.SubscriptionStatus = SubscriptionStatus.Active;
                        await _userRepository.UpdateAsync(user);

                        // Log în consola Docker ca să fim siguri
                        Console.WriteLine($"[STRIPE] Succes! Userul {userId} a devenit Premium.");
                    }
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            // Dacă ajungem aici, semnătura e greșită. Logăm eroarea pentru diagnostic.
            Console.WriteLine($"[STRIPE ERROR] Validare eșuată: {e.Message}");
            return BadRequest();
        }
    }
}