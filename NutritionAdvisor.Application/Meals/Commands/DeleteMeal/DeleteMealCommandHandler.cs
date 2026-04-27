using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Application.Meals.Commands.DeleteMeal;

public class DeleteMealCommandHandler : IRequestHandler<DeleteMealCommand>
{
    private readonly IDailyLogRepository _dailyLogRepository;

    public DeleteMealCommandHandler(IDailyLogRepository dailyLogRepository)
    {
        _dailyLogRepository = dailyLogRepository;
    }

    public async Task Handle(DeleteMealCommand request, CancellationToken cancellationToken)
    {
        var log = await _dailyLogRepository.GetByMealIdAsync(request.MealId, cancellationToken);
        if (log == null) throw new Exception("Meal not found.");

        if (log.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this meal.");
        }

        var meal = log.Meals.FirstOrDefault(m => m.Id == request.MealId);
        if (meal == null) throw new Exception("Meal not found in the daily log.");

        log.Meals.Remove(meal);
        await _dailyLogRepository.UpdateAsync(log, cancellationToken);

    }
}