namespace NutritionAdvisor.Domain.Entities;
using System.ComponentModel.DataAnnotations;
public record UserProfile
{ 
    [Key]
    required public Guid UserId { get; set; }
    required public string Name { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public double Height { get; set; }
    public double Weight { get; set; }
    public string? Allergies { get; set; }
    public string? Objective  { get; set; }
}