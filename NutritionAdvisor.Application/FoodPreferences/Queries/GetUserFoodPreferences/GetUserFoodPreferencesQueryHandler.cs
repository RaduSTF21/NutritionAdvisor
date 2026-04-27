using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.FoodPreferences.Queries.GetUserFoodPreferences;

public class GetUserFoodPreferencesQueryHandler : IRequestHandler<GetUserFoodPreferencesQuery, FoodPreference?>
{
    private readonly IFoodPreferenceRepository _repository;

    public GetUserFoodPreferencesQueryHandler(IFoodPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<FoodPreference?> Handle(GetUserFoodPreferencesQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
