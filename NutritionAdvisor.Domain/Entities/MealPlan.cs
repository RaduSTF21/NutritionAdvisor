using System.ComponentModel.DataAnnotations;

namespace NutritionAdvisor.Domain.Entities;

public class MealPlan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public DateTime Date { get; set; }

    public int TargetCalories { get; set; }

    public int TotalCalories { get; set; }

    public bool IsAIGenerated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<MealPlanItem> Items { get; set; } = new();
}