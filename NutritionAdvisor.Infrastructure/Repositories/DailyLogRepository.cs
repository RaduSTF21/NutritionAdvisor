using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;

namespace NutritionAdvisor.Infrastructure.Repositories;

public class DailyLogRepository : IDailyLogRepository
{
    private readonly ApplicationDbContext _dbContext;
    public DailyLogRepository(ApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<DailyLog?> GetByDateAsync(Guid userId, DateTime date, CancellationToken ct)
    {
        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        return await _dbContext.DailyLogs
            .Include(dl => dl.Meals)
                .ThenInclude(m => m.Recipe)
                .ThenInclude(r => r.Ingredients)
                .ThenInclude(i => i.Ingredient)
            .FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date.Date == utcDate, ct);
    }
    public async Task<DailyLog?> GetByMealIdAsync(Guid mealId, CancellationToken ct)
    {
        return await _dbContext.DailyLogs
            .Include(dl => dl.Meals)
                .ThenInclude(m => m.Recipe)
                .ThenInclude(r => r.Ingredients)
                .ThenInclude(i => i.Ingredient)
            .FirstOrDefaultAsync(dl => dl.Meals.Any(m => m.Id == mealId), ct);
    }
    public async Task AddAsync(DailyLog dailyLog, CancellationToken ct)
    {
        await _dbContext.DailyLogs.AddAsync(dailyLog, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DailyLog dailyLog, CancellationToken ct)
    {
        _dbContext.DailyLogs.Update(dailyLog);
        await _dbContext.SaveChangesAsync(ct);
    }
}