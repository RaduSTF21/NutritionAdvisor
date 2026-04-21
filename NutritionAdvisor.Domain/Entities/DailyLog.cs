namespace NutritionAdvisor.Domain.Entities;

public class DailyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime Date { get; set; }
    public Guid UserId { get; set; }
    public List<Meal> Meals { get; set; } = new List<Meal>();
}