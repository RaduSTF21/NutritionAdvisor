using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;

namespace NutritionAdvisor.Infrastructure.Repositories;

public class IngredientRepository : IIngredientRepository
{
    private readonly ApplicationDbContext _context;

    public IngredientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Return the full ingredient list.
    public async Task<IEnumerable<Ingredient>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Ingredients.ToListAsync(cancellationToken);
    }

    public async Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Ingredients.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<Ingredient>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken)
    {
        return await _context.Ingredients
            .Where(i => i.Name.ToLower().Contains(searchTerm.ToLower()))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        await _context.Ingredients.AddAsync(ingredient, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}