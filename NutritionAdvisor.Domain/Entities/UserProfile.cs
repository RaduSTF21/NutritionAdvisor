namespace NutritionAdvisor.Domain.Entities;

public record UserProfile
{
    required public Guid Id { get; init; }
    required public string Name { get; set; }
    required public string Gender { get; set; }
    public int Age { get; set; }
    public double Height { get; set; }
    public double Weight { get; set; }
    public string? Allergies { get; set; }
    public string? Objective  { get; set; }
}