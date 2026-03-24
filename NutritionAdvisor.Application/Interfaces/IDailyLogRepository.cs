using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IDailyLogRepository
{
    Task<DailyLog?> GetByDateAsync(Guid userId, DateTime date, CancellationToken ct);
    Task AddAsync(DailyLog dailyLog, CancellationToken ct);
    Task UpdateAsync(DailyLog dailyLog, CancellationToken ct);
}