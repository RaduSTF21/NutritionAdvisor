using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;
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
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IEnumerable<Recipe>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Recipes
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Ingredient)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Recipe>> FilterAsync(string? searchTerm, string? tag, Difficulty? level, CancellationToken ct)
    {
        var query = _context.Recipes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLowerInvariant();
            query = query.Where(r => (r.Title ?? string.Empty).ToLower().Contains(lowerTerm) || (r.Description ?? string.Empty).ToLower().Contains(lowerTerm));
        }

        if (!string.IsNullOrWhiteSpace(tag) && Enum.TryParse<Difficulty>(tag, true, out var parsedLevel))
        {
            query = query.Where(r => r.Level == parsedLevel);
        }

        if (level.HasValue)
        {
            query = query.Where(r => r.Level == level.Value);
        }

        return await query
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Ingredient)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Recipe recipe, CancellationToken ct)
    {
        await _context.Recipes.AddAsync(recipe, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var recipe = await _context.Recipes.FindAsync(new object[] { id }, ct);
        if (recipe != null)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(Recipe recipe, CancellationToken ct)
    {
        _context.Recipes.Update(recipe);
        await _context.SaveChangesAsync(ct);
    }
}