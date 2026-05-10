using MediatR;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal;

public class DeleteMealCommandHandler : IRequestHandler<DeleteMealCommand, bool>
{
    private readonly IDailyLogRepository _dailyLogRepository;

    public DeleteMealCommandHandler(IDailyLogRepository dailyLogRepository)
    {
        _dailyLogRepository = dailyLogRepository;
    }

    public async Task<bool> Handle(DeleteMealCommand request, CancellationToken cancellationToken)
    {
        // Get the daily log containing this meal
        var dailyLog = await _dailyLogRepository.GetByMealIdAsync(request.MealId, cancellationToken);
        if (dailyLog == null)
        {
            return false;
        }

        // Verify ownership
        if (dailyLog.UserId != request.UserId)
        {
            return false;
        }

        // Find and remove the meal
        var meal = dailyLog.Meals.FirstOrDefault(m => m.Id == request.MealId);
        if (meal == null)
        {
            return false;
        }

        dailyLog.Meals.Remove(meal);
        await _dailyLogRepository.UpdateAsync(dailyLog, cancellationToken);

        return true;
    }
}
