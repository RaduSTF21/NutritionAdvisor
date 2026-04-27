namespace NutritionAdvisor.Domain.Entities;

public class Meal
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime EatenAt { get; set; }
    public Guid LogId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public DailyLog DailyLog { get; set; } = null!;

}