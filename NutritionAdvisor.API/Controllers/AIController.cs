using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Enums;
using System.Security.Claims;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IPythonAiService _aiService;
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IAllergyRepository _allergyRepository;
    private readonly IFoodPreferenceRepository _foodPreferenceRepository;
    private readonly IRecipeRepository _recipeRepository;

    public AIController(
        IPythonAiService aiService,
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        IAllergyRepository allergyRepository,
        IFoodPreferenceRepository foodPreferenceRepository,
        IRecipeRepository recipeRepository)
    {
        _aiService = aiService;
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _allergyRepository = allergyRepository;
        _foodPreferenceRepository = foodPreferenceRepository;
        _recipeRepository = recipeRepository;
    }

    private async Task<bool> IsUserPremiumAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null
               && user.SubscriptionPlan == SubscriptionPlan.Premium
               && user.SubscriptionStatus == SubscriptionStatus.Active;
    }

    private async Task<(string? Objective, List<string> Allergies, List<string> Disliked, List<AiRecipeDto> Recipes, double? WeightKg, double? HeightCm, int? Age, string? Gender, string? DietType, List<string> PreferredCuisines)> GetUserContextAsync(Guid userId)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(userId);
        var objective = string.IsNullOrWhiteSpace(profile?.Objective)
            ? "General Health Improvement"
            : profile.Objective!.Trim();

        var allergies = await _allergyRepository.GetByUserIdAsync(userId, CancellationToken.None);
        var allergyNames = allergies.Select(a => a.AllergenName).ToList();

        if (!string.IsNullOrWhiteSpace(profile?.Allergies))
        {
            allergyNames.AddRange(
                profile.Allergies.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        allergyNames = allergyNames
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var preferences = await _foodPreferenceRepository.GetByUserIdAsync(userId, CancellationToken.None);
        var disliked = preferences?.DislikedIngredients ?? new List<string>();

        var recipesDb = await _recipeRepository.GetAllAsync(CancellationToken.None);
        var availableRecipes = recipesDb.Select(r => new AiRecipeDto(
            r.Id,
            r.Title ?? "Untitled Recipe",
            r.Description ?? string.Empty,
            r.Ingredients.Select(i => i.Ingredient?.Name ?? "Unknown").ToList(),
            (int)r.TotalCalories, // Proprietate calculată din Recipe[cite: 1]
            r.CookingTimeInMinutes,
            r.Level.ToString()
        )).ToList();

        return (
            objective,
            allergyNames,
            disliked,
            availableRecipes,
            profile?.Weight > 0 ? profile.Weight : null,
            profile?.Height > 0 ? profile.Height : null,
            profile?.Age > 0 ? profile.Age : null,
            string.IsNullOrWhiteSpace(profile?.Gender) ? null : profile.Gender,
            preferences?.DietType.ToString(),
            preferences?.PreferredCuisines ?? new List<string>());
    }

    [HttpPost("recommend-recipes")]
    public async Task<IActionResult> RecommendRecipes([FromBody] FrontendRecommendationRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var context = await GetUserContextAsync(userId);
        var objective = string.IsNullOrWhiteSpace(request.SearchQuery)
            ? context.Objective
            : request.SearchQuery.Trim();

        var aiRequest = new AiRecommendationRequestModel(
            userId.ToString(),
            objective,
            context.Allergies,
            context.Disliked,
            request.Limit,
            context.Recipes,
            request.SearchQuery,
            request.UseInternetSearch
        );

        var result = await _aiService.GetRecommendationsAsync(aiRequest);
        return Ok(result);
    }

    [HttpPost("generate-meal-plan")]
    public async Task<IActionResult> GenerateMealPlan([FromBody] FrontendMealPlanRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);

        if (!await IsUserPremiumAsync(userId))
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Premium subscription required." });

        var context = await GetUserContextAsync(userId);

        var aiRequest = new AiMealPlanRequestModel(
            userId.ToString(),
            context.Objective,
            request.Days,
            context.Allergies,
            context.Disliked,
            context.Recipes,
            context.WeightKg,
            context.HeightCm,
            context.Age,
            context.Gender,
            context.DietType,
            context.PreferredCuisines
        );

        var result = await _aiService.GenerateMealPlanAsync(aiRequest);
        return Ok(result);
    }

    [HttpPost("coach")]
    public async Task<IActionResult> AskCoach([FromBody] FrontendCoachRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);

        if (!await IsUserPremiumAsync(userId))
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Premium subscription required." });

        var context = await GetUserContextAsync(userId);

        var aiRequest = new AiCoachRequestModel(
            userId.ToString(),
            context.Objective,
            request.Message,
            request.Context,
            context.Allergies,
            context.Disliked
        );

        var result = await _aiService.GetCoachAdviceAsync(aiRequest);
        return Ok(result);
    }
}

// Modele pentru request-urile venite din Frontend (Blazor)[cite: 2]
public record FrontendRecommendationRequest(int Limit, string? SearchQuery = null, bool UseInternetSearch = false);
public record FrontendMealPlanRequest(int Days);
public record FrontendCoachRequest(string Message, string? Context);