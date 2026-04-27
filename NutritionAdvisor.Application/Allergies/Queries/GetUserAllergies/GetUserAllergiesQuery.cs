using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Allergies.Queries.GetUserAllergies;

public class GetUserAllergiesQuery : IRequest<IEnumerable<Allergy>>
{
    public Guid UserId { get; set; }

    public GetUserAllergiesQuery(Guid userId)
    {
        UserId = userId;
    }
}
