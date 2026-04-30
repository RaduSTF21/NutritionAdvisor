using Microsoft.Extensions.Configuration;
using NutritionAdvisor.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace NutritionAdvisor.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;

    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(string userEmail, string userId)
    {
        var domain = _configuration["Stripe:Domain"]; // http://localhost:5210 (Frontend)

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = 4900, // 49.00 RON
                        Currency = "ron",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Nutrition Advisor Premium - Abonament Lunar",
                            Description = "Acces complet la AI Coach, planuri de mese și analize avansate."
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = domain + "/payment-success",
            CancelUrl = domain + "/profil",
            CustomerEmail = userEmail,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId } // Important pentru Webhook!
            }
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options);

        return session.Url;
    }
}