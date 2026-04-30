namespace NutritionAdvisor.Domain.Entities;

using NutritionAdvisor.Domain.Enums;

public class User
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Inactive;
    public DateTime? SubscriptionStartAt { get; set; }
    public DateTime? SubscriptionEndAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool AutoRenew { get; set; }
    public string? ProviderCustomerId { get; set; }
    public string? ProviderSubscriptionId { get; set; }
}