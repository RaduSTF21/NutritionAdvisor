namespace NutritionAdvisor.Domain.Entities;

public class DailyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Date { get; set; }
    public Guid UserId { get; set; }
    public List<Recipe> ConsumedRecipes { get; set; } = new List<Recipe>();
}