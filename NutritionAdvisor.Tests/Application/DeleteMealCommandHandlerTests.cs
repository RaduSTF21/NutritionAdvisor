using NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal;
using NutritionAdvisor.Tests.TestDoubles;
using NutritionAdvisor.Domain.Entities;
using System;

namespace NutritionAdvisor.Tests.Application;

public class DeleteMealCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesMeal_WhenOwnerMatches()
    {
        var repo = new InMemoryDailyLogRepository();
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();

        var ingredient = new NutritionAdvisor.Domain.Entities.Ingredient { Id = Guid.NewGuid(), Name = "i1", Calories = 200 };
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Ingredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Ingredient = ingredient, IngredientId = ingredient.Id, Amount = 100, RecipeId = Guid.NewGuid() }
            }
        };
        var meal = new Meal { Id = mealId, Recipe = recipe, EatenAt = DateTime.UtcNow };
        var log = new DailyLog { Id = Guid.NewGuid(), UserId = userId, Date = DateTime.UtcNow.Date, Meals = new System.Collections.Generic.List<Meal> { meal } };

        await repo.AddAsync(log, CancellationToken.None);

        var handler = new NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal.DeleteMealCommandHandler(repo);

        var result = await handler.Handle(new NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal.DeleteMealCommand(userId, mealId), CancellationToken.None);

        Assert.True(result);
        var fetched = await repo.GetByMealIdAsync(mealId, CancellationToken.None);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenMealNotFound()
    {
        var repo = new InMemoryDailyLogRepository();
        var handler = new NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal.DeleteMealCommandHandler(repo);

        var result = await handler.Handle(new NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal.DeleteMealCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenNotOwner()
    {
        var repo = new InMemoryDailyLogRepository();
        var ownerId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var mealId = Guid.NewGuid();

        var ingredient2 = new NutritionAdvisor.Domain.Entities.Ingredient { Id = Guid.NewGuid(), Name = "i2", Calories = 150 };
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Ingredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Ingredient = ingredient2, IngredientId = ingredient2.Id, Amount = 100, RecipeId = Guid.NewGuid() }
            }
        };
        var meal = new Meal { Id = mealId, Recipe = recipe, EatenAt = DateTime.UtcNow };
        var log = new DailyLog { Id = Guid.NewGuid(), UserId = ownerId, Date = DateTime.UtcNow.Date, Meals = new System.Collections.Generic.List<Meal> { meal } };

        await repo.AddAsync(log, CancellationToken.None);

        var handler = new NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal.DeleteMealCommandHandler(repo);
        var result = await handler.Handle(new NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal.DeleteMealCommand(otherUser, mealId), CancellationToken.None);

        Assert.False(result);
        var fetched = await repo.GetByMealIdAsync(mealId, CancellationToken.None);
        Assert.NotNull(fetched);
    }
}
