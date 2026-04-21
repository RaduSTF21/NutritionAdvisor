using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Allergies.Queries.GetUserAllergies;

public class GetUserAllergiesQueryHandler : IRequestHandler<GetUserAllergiesQuery, IEnumerable<Allergy>>
{
    private readonly IAllergyRepository _repository;

    public GetUserAllergiesQueryHandler(IAllergyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Allergy>> Handle(GetUserAllergiesQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
