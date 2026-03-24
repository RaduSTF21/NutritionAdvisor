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
        return await _dbContext.DailyLogs
            .Include(dl => dl.ConsumedRecipes)
            .FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date.Date == date.Date, ct);
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