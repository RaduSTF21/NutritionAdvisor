using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Domain.Entities;

public class Allergy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string AllergenName { get; set; } = string.Empty;
    public AllergySeverity SeverityLevel { get; set; } = AllergySeverity.Mild;
}
