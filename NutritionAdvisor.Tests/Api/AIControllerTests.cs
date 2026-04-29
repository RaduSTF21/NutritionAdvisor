using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Tests.Api;

public class AIControllerTests
{
    [Fact]
    public async Task RecommendRecipes_ReturnsFreeMode_AndFiltersBlockedRecipes()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var recipeRepository = new Mock<IRecipeRepository>();
        recipeRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateRecipe("Salată cu pui", 420, "pui", "salată"),
                CreateRecipe("Paste cu bacon", 760, "bacon", "ou")
            });

        var profileRepository = new Mock<IUserProfileRepository>();
        profileRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                UserId = userId,
                Name = "Radu",
                Objective = "Weight Loss"
            });

        var preferenceRepository = new Mock<IFoodPreferenceRepository>();
        preferenceRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodPreference
            {
                UserId = userId,
                DislikedIngredients = new List<string> { "bacon" }
            });

        var allergyRepository = new Mock<IAllergyRepository>();
        allergyRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Allergy>());

        var controller = CreateController(
            userId,
            "Free",
            "Inactive",
            DateTime.UtcNow.AddDays(-1),
            recipeRepository.Object,
            profileRepository.Object,
            preferenceRepository.Object,
            allergyRepository.Object,
            Mock.Of<IPythonAiService>());

        var result = await controller.RecommendRecipes(new AiRecommendationRequestDto { Limit = 5 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AiRecommendationResponseDto>(ok.Value);

        Assert.Equal("Free", payload.Mode);
        Assert.Single(payload.Recommendations);
        Assert.Equal("Salată cu pui", payload.Recommendations[0].Title);
        Assert.False(payload.Recommendations[0].Premium);
    }

    [Fact]
    public async Task RecommendRecipes_ReturnsPremiumMode_AndUsesPythonService()
    {
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var recipeRepository = new Mock<IRecipeRepository>();
        recipeRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Recipe>());

        var profileRepository = new Mock<IUserProfileRepository>();
        profileRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                UserId = userId,
                Name = "Radu",
                Objective = "Muscle Gain"
            });

        var preferenceRepository = new Mock<IFoodPreferenceRepository>();
        preferenceRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodPreference
            {
                UserId = userId,
                DislikedIngredients = new List<string>()
            });

        var allergyRepository = new Mock<IAllergyRepository>();
        allergyRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Allergy>());

        var pythonAiService = new Mock<IPythonAiService>();
        pythonAiService.Setup(service => service.RecommendRecipesAsync(It.IsAny<AiContextDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new AiRecommendationItemDto("1", "Paste proteice", "Meal premium", new[] { "paste", "pui" }, "AI premium", true, 25)
            });

        var controller = CreateController(
            userId,
            "Premium",
            "Active",
            DateTime.UtcNow.AddDays(7),
            recipeRepository.Object,
            profileRepository.Object,
            preferenceRepository.Object,
            allergyRepository.Object,
            pythonAiService.Object);

        var result = await controller.RecommendRecipes(new AiRecommendationRequestDto { Limit = 3 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AiRecommendationResponseDto>(ok.Value);

        Assert.Equal("Premium", payload.Mode);
        Assert.Single(payload.Recommendations);
        Assert.True(payload.Recommendations[0].Premium);
        pythonAiService.Verify(service => service.RecommendRecipesAsync(It.IsAny<AiContextDto>(), 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateMealPlan_ReturnsPremiumMealPlan()
    {
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var controller = CreateController(
            userId,
            "Premium",
            "Active",
            DateTime.UtcNow.AddDays(7),
            Mock.Of<IRecipeRepository>(),
            Mock.Of<IUserProfileRepository>(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()) == Task.FromResult<UserProfile?>(new UserProfile { UserId = userId, Name = "Radu", Objective = "Weight Loss" })),
            Mock.Of<IFoodPreferenceRepository>(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()) == Task.FromResult<FoodPreference?>(new FoodPreference { UserId = userId })),
            Mock.Of<IAllergyRepository>(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()) == Task.FromResult<IEnumerable<Allergy>>(Array.Empty<Allergy>())),
            Mock.Of<IPythonAiService>(service => service.GenerateMealPlanAsync(It.IsAny<AiContextDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AiMealPlanResponseDto(userId, "Premium", "Plan generat", new[] { new AiMealPlanDayDto("day-1", "Salată cu pui", "Ușoară și rapidă.", 420) })))
        );

        var result = await controller.GenerateMealPlan(new AiMealPlanRequestDto { Days = 4 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AiMealPlanResponseDto>(ok.Value);

        Assert.Equal("Premium", payload.Mode);
        Assert.Single(payload.Days);
        Assert.Equal("Plan generat", payload.Summary);
    }

    private static AIController CreateController(
        Guid userId,
        string subscriptionPlan,
        string subscriptionStatus,
        DateTime expiresAt,
        IRecipeRepository recipeRepository,
        IUserProfileRepository profileRepository,
        IFoodPreferenceRepository preferenceRepository,
        IAllergyRepository allergyRepository,
        IPythonAiService pythonAiService)
    {
        var controller = new AIController(recipeRepository, profileRepository, preferenceRepository, allergyRepository, pythonAiService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim("subscription_plan", subscriptionPlan),
                        new Claim("subscription_status", subscriptionStatus),
                        new Claim("subscription_expires_at", expiresAt.ToString("O"))
                    }, "Bearer"))
                }
            }
        };

        return controller;
    }

    private static Recipe CreateRecipe(string title, float calories, params string[] ingredientNames)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = title,
            CookingTimeInMinutes = 25,
            Level = Difficulty.Easy,
            Instructions = "Mix"
        };

        recipe.Ingredients = ingredientNames.Select(name => new RecipeIngredient
        {
            Ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = name,
                Calories = calories / ingredientNames.Length,
                Protein = 10,
                Carbohydrates = 12,
                Fats = 4,
                Fiber = 2,
                Sugar = 1,
                Sodium = 100,
                IsVegan = !name.Equals("pui", StringComparison.OrdinalIgnoreCase) && !name.Equals("bacon", StringComparison.OrdinalIgnoreCase)
            },
            Amount = 100,
            Unit = "g"
        }).ToList();

        return recipe;
    }
}
