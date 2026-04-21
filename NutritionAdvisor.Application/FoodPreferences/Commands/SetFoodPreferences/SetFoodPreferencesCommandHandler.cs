using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.FoodPreferences.Commands.SetFoodPreferences;

public class SetFoodPreferencesCommandHandler : IRequestHandler<SetFoodPreferencesCommand, Guid>
{
    private readonly IFoodPreferenceRepository _repository;

    public SetFoodPreferencesCommandHandler(IFoodPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(SetFoodPreferencesCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (existing == null)
        {
            var newPreference = new FoodPreference
            {
                UserId = request.UserId,
                DietType = request.DietType,
                PreferredCuisines = request.PreferredCuisines,
                DislikedIngredients = request.DislikedIngredients
            };

            await _repository.AddAsync(newPreference, cancellationToken);
            return newPreference.Id;
        }

        existing.DietType = request.DietType;
        existing.PreferredCuisines = request.PreferredCuisines;
        existing.DislikedIngredients = request.DislikedIngredients;

        await _repository.UpdateAsync(existing, cancellationToken);
        return existing.Id;
    }
}
