using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Domain.Entities;

public class FoodPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DietType DietType { get; set; } = DietType.None;
    public List<string> PreferredCuisines { get; set; } = new();
    public List<string> DislikedIngredients { get; set; } = new();
}
