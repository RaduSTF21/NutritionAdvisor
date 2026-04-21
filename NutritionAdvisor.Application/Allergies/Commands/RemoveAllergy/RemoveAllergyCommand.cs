using MediatR;

namespace NutritionAdvisor.Application.Allergies.Commands.RemoveAllergy;

public class RemoveAllergyCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid AllergyId { get; set; }

    public RemoveAllergyCommand(Guid userId, Guid allergyId)
    {
        UserId = userId;
        AllergyId = allergyId;
    }
}
