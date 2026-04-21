using MediatR;

using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.DailyLogs.Commands.LogMeal;

public class LogMealCommandHandler : IRequestHandler<LogMealCommand, Guid>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IDailyLogRepository _dailyLogRepository;
    public LogMealCommandHandler(IRecipeRepository recipeRepository, IDailyLogRepository dailyLogRepository)
    {
        _recipeRepository = recipeRepository;
        _dailyLogRepository = dailyLogRepository;
    }

    public async Task<Guid> Handle(LogMealCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(request.RecipeId, cancellationToken) ?? throw new Exception("Recipe not found.");
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);


        var log = await _dailyLogRepository.GetByDateAsync(request.UserId, today, cancellationToken);
        if (log == null)
        {
            log = new DailyLog
            {
                UserId = request.UserId,
                Date = today,
                Meals = new List<Meal> { new Meal { Recipe = recipe, EatenAt = DateTime.UtcNow } }
            };
            await _dailyLogRepository.AddAsync(log, cancellationToken);
        }
        else
        {
            log.Meals.Add(new Meal
            {
                Recipe = recipe,
                EatenAt = DateTime.UtcNow
            });
            await _dailyLogRepository.UpdateAsync(log, cancellationToken);
        }
        return log.Id;
    }
}