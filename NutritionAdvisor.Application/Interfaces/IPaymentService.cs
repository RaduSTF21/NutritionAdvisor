namespace NutritionAdvisor.Application.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(string userEmail, string userId);
}