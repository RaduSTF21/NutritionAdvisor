using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Domain.Entities;

public class MealPlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MealPlanId { get; set; }

    public MealType MealType { get; set; }

    public Guid? RecipeId { get; set; }

    public Recipe? Recipe { get; set; }

    public string? ExternalTitle { get; set; }

    public string? ExternalUrl { get; set; }

    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbs { get; set; }
    public float Fats { get; set; }

    public MealPlan MealPlan { get; set; } = null!;
}