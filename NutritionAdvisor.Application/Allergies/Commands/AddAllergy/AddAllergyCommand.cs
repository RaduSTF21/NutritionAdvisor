using MediatR;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.Allergies.Commands.AddAllergy;

public class AddAllergyCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public string AllergenName { get; set; } = string.Empty;
    public AllergySeverity SeverityLevel { get; set; } = AllergySeverity.Mild;
}
