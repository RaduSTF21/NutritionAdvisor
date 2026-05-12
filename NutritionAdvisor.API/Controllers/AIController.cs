using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Enums;
using System.Security.Claims;
using System.Text.Json.Serialization;

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

    // CA1861 Fix: Array static pentru fallback
    private static readonly string[] EmptyStringArray = Array.Empty<string>();

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
        return user != null && user.SubscriptionPlan == SubscriptionPlan.Premium && user.SubscriptionStatus == SubscriptionStatus.Active;
    }

    private async Task<UserAiContext> GetUserContextAsync(Guid userId)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(userId, CancellationToken.None);
        var allergies = await _allergyRepository.GetByUserIdAsync(userId, CancellationToken.None);
        var preferences = await _foodPreferenceRepository.GetByUserIdAsync(userId, CancellationToken.None);

        var recipes = await _recipeRepository.GetAllAsync(CancellationToken.None);
        var availableRecipes = recipes.Select(r => new AiRecipeDto(
            r.Id,
            r.Title ?? string.Empty,
            r.Description ?? string.Empty,
            r.Ingredients.Select(i => $"{i.Amount}{i.Unit} {i.Ingredient?.Name}").ToList(),
            (int)r.TotalCalories,
            r.CookingTimeInMinutes,
            r.Tags.Count > 0 ? string.Join(", ", r.Tags) : string.Empty
        )).ToList();

        return new UserAiContext(
            Objective: string.IsNullOrWhiteSpace(profile?.Objective) ? "General Health Improvement" : profile!.Objective,
            WeightKg: profile != null ? (float?)profile.Weight : null,
            HeightCm: profile != null ? (float?)profile.Height : null,
            Age: profile?.Age,
            Gender: profile?.Gender,
            Allergies: allergies?.Select(a => a.AllergenName).ToList() ?? EmptyStringArray.ToList(),
            Disliked: preferences?.DislikedIngredients ?? EmptyStringArray.ToList(),
            DietType: preferences?.DietType.ToString(),
            PreferredCuisines: preferences?.PreferredCuisines ?? EmptyStringArray.ToList(),
            Recipes: availableRecipes
        );
    }

    private sealed record UserAiContext(
        string? Objective, float? WeightKg, float? HeightCm, int? Age, string? Gender,
        List<string> Allergies, List<string> Disliked, string? DietType,
        List<string> PreferredCuisines, List<AiRecipeDto> Recipes);

    [HttpPost("recommend-recipes")]
    public async Task<IActionResult> RecommendRecipes([FromBody] FrontendRecommendationRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var context = await GetUserContextAsync(userId);

        var objectiveToUse = string.IsNullOrWhiteSpace(request.SearchQuery) ? context.Objective : request.SearchQuery.Trim();

        var aiRequest = new AiRecommendationRequestModel(
            UserId: userId.ToString(),
            Objective: objectiveToUse,
            Allergies: context.Allergies,
            DislikedIngredients: context.Disliked,
            Limit: request.Limit,
            AvailableRecipes: context.Recipes,
            SearchQuery: request.SearchQuery,
            UseInternetSearch: request.UseInternetSearch
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
            return StatusCode(403, new { Error = "Premium subscription required." });

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
            return StatusCode(403, new { Error = "Premium subscription required." });

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

    [HttpPost("prewarm")]
    public async Task<IActionResult> Prewarm()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var context = await GetUserContextAsync(userId);

        var aiRequest = new AiMealPlanRequestModel(
            userId.ToString(),
            context.Objective,
            1,
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

        await _aiService.PrewarmAsync(aiRequest);
        return Accepted(new { Message = "Prewarm started" });
    }
}

public record FrontendRecommendationRequest([property: JsonRequired] int Limit, string? SearchQuery = null, bool UseInternetSearch = false);
public record FrontendMealPlanRequest([property: JsonRequired] int Days);
public record FrontendCoachRequest(string Message, string? Context);