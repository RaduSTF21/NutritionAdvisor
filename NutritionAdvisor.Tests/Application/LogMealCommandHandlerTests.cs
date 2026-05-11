using NutritionAdvisor.Application.DailyLogs.Commands.LogMeal;
using NutritionAdvisor.Tests.TestDoubles;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Tests.Application;

public class LogMealCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesDailyLog_WhenNoneExists()
    {
        var recipeRepo = new InMemoryRecipeRepository();
        var dailyRepo = new InMemoryDailyLogRepository();

        var ingredient = new NutritionAdvisor.Domain.Entities.Ingredient { Id = Guid.NewGuid(), Name = "meal-a-ing", Calories = 300 };
        var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Meal A", Ingredients = new List<RecipeIngredient> { new RecipeIngredient { Ingredient = ingredient, IngredientId = ingredient.Id, Amount = 100, RecipeId = Guid.NewGuid() } } };
        await recipeRepo.AddAsync(recipe, CancellationToken.None);

        var handler = new LogMealCommandHandler(recipeRepo, dailyRepo);
        var userId = Guid.NewGuid();

        var result = await handler.Handle(new LogMealCommand(userId, recipe.Id), CancellationToken.None);

        var log = await dailyRepo.GetByDateAsync(userId, DateTime.UtcNow.Date, CancellationToken.None);
        Assert.NotNull(log);
        Assert.Single(log.Meals);
        Assert.Equal(recipe.Id, log.Meals[0].Recipe.Id);
    }
}
