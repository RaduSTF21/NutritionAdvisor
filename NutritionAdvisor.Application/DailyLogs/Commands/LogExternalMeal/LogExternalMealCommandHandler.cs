using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.DailyLogs.Commands.LogExternalMeal;

public sealed class LogExternalMealCommandHandler : IRequestHandler<LogExternalMealCommand, Guid>
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IDailyLogRepository _dailyLogRepository;

    public LogExternalMealCommandHandler(
        IIngredientRepository ingredientRepository,
        IRecipeRepository recipeRepository,
        IDailyLogRepository dailyLogRepository)
    {
        _ingredientRepository = ingredientRepository;
        _recipeRepository = recipeRepository;
        _dailyLogRepository = dailyLogRepository;
    }

    public async Task<Guid> Handle(LogExternalMealCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var mealTitle = string.IsNullOrWhiteSpace(request.Title) ? "External recipe" : request.Title.Trim();
        var sourceNote = string.IsNullOrWhiteSpace(request.ExternalUrl)
            ? "Imported from an external recipe link."
            : $"Imported from {request.ExternalUrl.Trim()}";

        var snapshotIngredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = $"External meal snapshot - {mealTitle}",
            Calories = request.Calories,
            Protein = request.Protein,
            Carbohydrates = request.Carbs,
            Fats = request.Fats,
            Fiber = 0,
            Sugar = 0,
            Sodium = 0
        };

        await _ingredientRepository.AddAsync(snapshotIngredient, cancellationToken);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = mealTitle,
            Description = sourceNote,
            CookingTimeInMinutes = 1,
            Level = Difficulty.Easy,
            Instructions = sourceNote,
            Tags = new List<string> { "external", "imported" },
            Ingredients = new List<RecipeIngredient>
            {
                new()
                {
                    RecipeId = Guid.NewGuid(),
                    IngredientId = snapshotIngredient.Id,
                    Amount = 100,
                    Unit = "g"
                }
            }
        };

        recipe.Ingredients[0].RecipeId = recipe.Id;

        await _recipeRepository.AddAsync(recipe, cancellationToken);

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
