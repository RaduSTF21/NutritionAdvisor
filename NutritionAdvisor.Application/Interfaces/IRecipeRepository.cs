using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.Interfaces;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<Recipe>> FilterAsync(string? searchTerm, string? tag, Difficulty? level, CancellationToken ct);
    Task AddAsync(Recipe recipe, CancellationToken ct);
    Task UpdateAsync(Recipe recipe, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}