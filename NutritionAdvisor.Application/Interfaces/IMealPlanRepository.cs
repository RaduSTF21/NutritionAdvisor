using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<MealPlan?> GetByUserAndDateAsync(Guid userId, DateTime date, CancellationToken ct);
    Task<IEnumerable<MealPlan>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(MealPlan mealPlan, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}