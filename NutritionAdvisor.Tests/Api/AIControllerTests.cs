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
    public async Task RecommendRecipes_ForwardsUserContextAndReturnsServiceResponse()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                UserId = userId,
                SubscriptionPlan = SubscriptionPlan.Free,
                SubscriptionStatus = SubscriptionStatus.Inactive
            });

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

        AiRecommendationRequestModel? capturedRequest = null;
        var pythonAiService = new Mock<IPythonAiService>();
        pythonAiService.Setup(service => service.GetRecommendationsAsync(It.IsAny<AiRecommendationRequestModel>()))
            .Callback<AiRecommendationRequestModel>(request => capturedRequest = request)
            .ReturnsAsync(new AiRecommendationResponseModel(
                userId,
                "Free",
                new List<AiRecommendationItemModel>
                {
                    new("1", "Salată cu pui", "Meal free", new List<string> { "pui", "salată" }, "Selected locally.", false, 25)
                }));

        var controller = CreateController(
            userId,
            "Free",
            "Inactive",
            DateTime.UtcNow.AddDays(-1),
            userRepository.Object,
            recipeRepository.Object,
            profileRepository.Object,
            preferenceRepository.Object,
            allergyRepository.Object,
            pythonAiService.Object);

        var result = await controller.RecommendRecipes(new FrontendRecommendationRequest(5));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AiRecommendationResponseModel>(ok.Value);

        Assert.Equal("Free", payload.Mode);
        Assert.Single(payload.Recommendations);
        Assert.Equal("Salată cu pui", payload.Recommendations[0].Title);
        Assert.False(payload.Recommendations[0].Premium);
        Assert.NotNull(capturedRequest);
        Assert.Equal(5, capturedRequest!.Limit);
        Assert.Equal(userId.ToString(), capturedRequest.UserId);
        Assert.Equal("Weight Loss", capturedRequest.Objective);
        Assert.Equal(2, capturedRequest.AvailableRecipes.Count);
        pythonAiService.Verify(service => service.GetRecommendationsAsync(It.IsAny<AiRecommendationRequestModel>()), Times.Once);
    }

    [Fact]
    public async Task RecommendRecipes_ReturnsPremiumMode_AndUsesPythonService()
    {
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                UserId = userId,
                SubscriptionPlan = SubscriptionPlan.Premium,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionEndAt = DateTime.UtcNow.AddDays(7)
            });

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

        AiRecommendationRequestModel? capturedRequest = null;
        var pythonAiService = new Mock<IPythonAiService>();
        pythonAiService.Setup(service => service.GetRecommendationsAsync(It.IsAny<AiRecommendationRequestModel>()))
            .Callback<AiRecommendationRequestModel>(request => capturedRequest = request)
            .ReturnsAsync(new AiRecommendationResponseModel(
                userId,
                "Premium",
                new List<AiRecommendationItemModel>
                {
                    new("1", "Paste proteice", "Meal premium", new List<string> { "paste", "pui" }, "AI premium", true, 25)
                }));

        var controller = CreateController(
            userId,
            "Premium",
            "Active",
            DateTime.UtcNow.AddDays(7),
            userRepository.Object,
            recipeRepository.Object,
            profileRepository.Object,
            preferenceRepository.Object,
            allergyRepository.Object,
            pythonAiService.Object);

        var result = await controller.RecommendRecipes(new FrontendRecommendationRequest(3));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AiRecommendationResponseModel>(ok.Value);

        Assert.Equal("Premium", payload.Mode);
        Assert.Single(payload.Recommendations);
        Assert.True(payload.Recommendations[0].Premium);
        Assert.NotNull(capturedRequest);
        Assert.Equal(3, capturedRequest!.Limit);
        pythonAiService.Verify(service => service.GetRecommendationsAsync(It.IsAny<AiRecommendationRequestModel>()), Times.Once);
    }

    [Fact]
    public async Task GenerateMealPlan_ReturnsPremiumMealPlan()
    {
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                UserId = userId,
                SubscriptionPlan = SubscriptionPlan.Premium,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionEndAt = DateTime.UtcNow.AddDays(7)
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
                DislikedIngredients = new List<string>()
            });

        var allergyRepository = new Mock<IAllergyRepository>();
        allergyRepository.Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Allergy>());

        AiMealPlanRequestModel? capturedRequest = null;
        var pythonAiService = new Mock<IPythonAiService>();
        pythonAiService.Setup(service => service.GenerateMealPlanAsync(It.IsAny<AiMealPlanRequestModel>()))
            .Callback<AiMealPlanRequestModel>(request => capturedRequest = request)
            .ReturnsAsync(new AiMealPlanResponseModel(
                userId,
                "Premium",
                "Plan generat",
                new List<AiMealPlanDayModel>
                {
                    new(
                        "2026-05-10",
                        "Day 1",
                        "Ușoară și rapidă.",
                        420,
                        new List<AiMealPlanMealModel>
                        {
                            new("Breakfast", null, "Salată cu pui", null, 420, 30, 20, 15)
                        })
                }));

        var recipeRepository = new Mock<IRecipeRepository>();
        recipeRepository.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Recipe>());

        var controller = CreateController(
            userId,
            "Premium",
            "Active",
            DateTime.UtcNow.AddDays(7),
            userRepository.Object,
            recipeRepository.Object,
            profileRepository.Object,
            preferenceRepository.Object,
            allergyRepository.Object,
            pythonAiService.Object);

        var result = await controller.GenerateMealPlan(new FrontendMealPlanRequest(4));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AiMealPlanResponseModel>(ok.Value);

        Assert.Equal("Premium", payload.Mode);
        Assert.Single(payload.Days);
        Assert.Equal("Plan generat", payload.Summary);
        Assert.NotNull(capturedRequest);
        Assert.Equal(4, capturedRequest!.Days);
        Assert.Equal(userId.ToString(), capturedRequest.UserId);
        pythonAiService.Verify(service => service.GenerateMealPlanAsync(It.IsAny<AiMealPlanRequestModel>()), Times.Once);
    }

    private static AIController CreateController(
        Guid userId,
        string subscriptionPlan,
        string subscriptionStatus,
        DateTime expiresAt,
        IUserRepository userRepository,
        IRecipeRepository recipeRepository,
        IUserProfileRepository profileRepository,
        IFoodPreferenceRepository preferenceRepository,
        IAllergyRepository allergyRepository,
        IPythonAiService pythonAiService)
    {
        var controller = new AIController(
            pythonAiService,
            userRepository,
            profileRepository,
            allergyRepository,
            preferenceRepository,
            recipeRepository)
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
