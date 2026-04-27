using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IFoodPreferenceRepository
{
    Task<FoodPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(FoodPreference foodPreference, CancellationToken cancellationToken);
    Task UpdateAsync(FoodPreference foodPreference, CancellationToken cancellationToken);
}
