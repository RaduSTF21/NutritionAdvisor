using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;

namespace NutritionAdvisor.Infrastructure.Repositories;

public class RecipeRepository : IRecipeRepository
{
    private readonly ApplicationDbContext _context;

    public RecipeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Recipes
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Ingredient) // Required for calculated nutrition properties.
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Recipes
            .Include(r => r.Ingredients)             // Load the join rows.
            .ThenInclude(ri => ri.Ingredient)        // Load the related ingredient data.
            .ToListAsync(ct);
    }

    public async Task AddAsync(Recipe recipe, CancellationToken ct)
    {
        await _context.Recipes.AddAsync(recipe, ct);
        await _context.SaveChangesAsync(ct);
    }

    // Update an existing recipe.
    public async Task UpdateAsync(Recipe recipe, CancellationToken ct)
    {
        _context.Recipes.Update(recipe);
        await _context.SaveChangesAsync(ct);
    }

    // Delete a recipe by ID.
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var recipe = await _context.Recipes.FindAsync(new object[] { id }, ct);
        if (recipe != null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync(ct);
        }
    }
}