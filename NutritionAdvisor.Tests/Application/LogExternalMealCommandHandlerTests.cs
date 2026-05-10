using NutritionAdvisor.Application.DailyLogs.Commands.LogExternalMeal;
using NutritionAdvisor.Tests.TestDoubles;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Tests.Application;

public class LogExternalMealCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesSnapshotRecipe_AndLogsMeal()
    {
        var ingredientRepo = new InMemoryIngredientRepository();
        var recipeRepo = new InMemoryRecipeRepository();
        var dailyRepo = new InMemoryDailyLogRepository();

        var handler = new LogExternalMealCommandHandler(ingredientRepo, recipeRepo, dailyRepo);
        var userId = Guid.NewGuid();

        var command = new LogExternalMealCommand(userId, "External Test", "http://example.com/1", 250, 10, 30, 5);
        var result = await handler.Handle(command, CancellationToken.None);

        var log = await dailyRepo.GetByDateAsync(userId, DateTime.UtcNow.Date, CancellationToken.None);
        Assert.NotNull(log);
        Assert.Single(log.Meals);
        var meal = log.Meals[0];
        Assert.NotNull(meal.Recipe);
        Assert.Contains("external", meal.Recipe.Tags);
    }
}
