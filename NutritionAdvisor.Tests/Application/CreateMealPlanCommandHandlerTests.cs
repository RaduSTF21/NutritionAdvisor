using NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;
using NutritionAdvisor.Tests.TestDoubles;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Tests.Application;

public class CreateMealPlanCommandHandlerTests
{
    [Fact]
    public async Task Handle_SavesMealPlan_WithLocalAndExternalItems()
    {
        var mealPlanRepo = new InMemoryMealPlanRepository();
        var recipeRepo = new InMemoryRecipeRepository();

        var ingredient = new NutritionAdvisor.Domain.Entities.Ingredient { Id = Guid.NewGuid(), Name = "local-ing", Calories = 400 };
        var existingRecipe = new Recipe { Id = Guid.NewGuid(), Title = "Local Recipe", Ingredients = new List<RecipeIngredient> { new RecipeIngredient { Ingredient = ingredient, IngredientId = ingredient.Id, Amount = 100, RecipeId = Guid.NewGuid() } } };
        await recipeRepo.AddAsync(existingRecipe, CancellationToken.None);

        var handler = new CreateMealPlanCommandHandler(mealPlanRepo, recipeRepo);

        var cmd = new CreateMealPlanCommand
        {
            UserId = Guid.NewGuid(),
            Date = DateTime.UtcNow.Date,
            TargetCalories = 2000,
            IsAIGenerated = false,
            ReplaceExisting = false,
            Items = new List<CreateMealPlanItemDto>
            {
                new() { MealType = MealType.Breakfast, RecipeId = existingRecipe.Id, Calories = 0 },
                new() { MealType = MealType.Lunch, Title = "External Delight", ExternalUrl = "http://example.com/recipe", Calories = 500 }
            }
        };

        var id = await handler.Handle(cmd, CancellationToken.None);

        var saved = await mealPlanRepo.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal(2, saved.Items.Count);
        Assert.Equal(existingRecipe.Id, saved.Items.First().RecipeId);
        Assert.Equal("External Delight", saved.Items.Last().ExternalTitle);
    }
}
