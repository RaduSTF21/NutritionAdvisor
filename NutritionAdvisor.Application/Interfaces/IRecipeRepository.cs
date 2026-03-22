using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Recipe recipe, CancellationToken ct);
}