using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IIngredientRepository
{
    Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    
    // Required to load the full ingredient list when no search term is provided.
    Task<IEnumerable<Ingredient>> GetAllAsync(CancellationToken cancellationToken);
    
    Task<IEnumerable<Ingredient>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken);
    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken);
}