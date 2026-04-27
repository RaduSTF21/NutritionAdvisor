using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;

namespace NutritionAdvisor.Infrastructure.Repositories;

public class FoodPreferenceRepository : IFoodPreferenceRepository
{
    private readonly ApplicationDbContext _context;

    public FoodPreferenceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FoodPreference?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.FoodPreferences
            .FirstOrDefaultAsync(fp => fp.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(FoodPreference foodPreference, CancellationToken cancellationToken)
    {
        await _context.FoodPreferences.AddAsync(foodPreference, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FoodPreference foodPreference, CancellationToken cancellationToken)
    {
        _context.FoodPreferences.Update(foodPreference);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
