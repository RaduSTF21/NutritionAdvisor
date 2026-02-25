namespace NutritionAdvisor.Domain.Entities;

public class Reteta
{
    public Guid Id { get; init; }
    required public string Title { get; set; }
    public int Calories { get; set; }
    public int CookingTime { get; set; }
    required public string ImageURL { get; set; }
    
}