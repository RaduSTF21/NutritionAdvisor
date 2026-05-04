using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;

namespace NutritionAdvisor.Infrastructure.Repositories;

public class MealPlanRepository : IMealPlanRepository
{
    private readonly ApplicationDbContext _context;

    public MealPlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.MealPlans
            .Include(plan => plan.Items)
            .ThenInclude(item => item.Recipe)
            .FirstOrDefaultAsync(plan => plan.Id == id, ct);
    }

    public async Task<MealPlan?> GetByUserAndDateAsync(Guid userId, DateTime date, CancellationToken ct)
    {
        var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

        return await _context.MealPlans
            .Include(plan => plan.Items)
            .ThenInclude(item => item.Recipe)
            .FirstOrDefaultAsync(plan => plan.UserId == userId && plan.Date.Date == normalizedDate, ct);
    }

    public async Task<IEnumerable<MealPlan>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.MealPlans
            .Where(plan => plan.UserId == userId)
            .Include(plan => plan.Items)
            .ThenInclude(item => item.Recipe)
            .OrderByDescending(plan => plan.Date)
            .ToListAsync(ct);
    }

    public async Task AddAsync(MealPlan mealPlan, CancellationToken ct)
    {
        await _context.MealPlans.AddAsync(mealPlan, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var mealPlan = await _context.MealPlans.FirstOrDefaultAsync(plan => plan.Id == id, ct);
        if (mealPlan == null)
        {
            return;
        }

        _context.MealPlans.Remove(mealPlan);
        await _context.SaveChangesAsync(ct);
    }
}