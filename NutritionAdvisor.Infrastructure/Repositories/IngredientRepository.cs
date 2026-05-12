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

    public async Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Ingredient>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Ingredients
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        await _context.Ingredients.AddAsync(ingredient, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Ingredient>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _context.Ingredients
                .OrderBy(i => i.Name)
                .Take(20)
                .ToListAsync(cancellationToken);
        }

        var lowerTerm = searchTerm.ToLowerInvariant();

        return await _context.Ingredients
            .Where(i => i.Name.ToLower().Contains(lowerTerm))
            .OrderBy(i => i.Name)
            .Take(20)
            .ToListAsync(cancellationToken);
    }
}