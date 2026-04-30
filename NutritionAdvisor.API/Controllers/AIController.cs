using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IFoodPreferenceRepository _foodPreferenceRepository;
    private readonly IAllergyRepository _allergyRepository;
    private readonly IPythonAiService _pythonAiService;

    public AIController(
        IRecipeRepository recipeRepository,
        IUserProfileRepository userProfileRepository,
        IFoodPreferenceRepository foodPreferenceRepository,
        IAllergyRepository allergyRepository,
        IPythonAiService pythonAiService)
    {
        _recipeRepository = recipeRepository;
        _userProfileRepository = userProfileRepository;
        _foodPreferenceRepository = foodPreferenceRepository;
        _allergyRepository = allergyRepository;
        _pythonAiService = pythonAiService;
    }

    [HttpPost("recommend-recipes")]
    public async Task<IActionResult> RecommendRecipes([FromBody] AiRecommendationRequestDto request, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(cancellationToken);
        if (context == null)
        {
            return Unauthorized();
        }

        var subscription = GetSubscriptionState();
        if (subscription.IsPremium)
        {
            var recommendations = await _pythonAiService.RecommendRecipesAsync(context, NormalizeLimit(request.Limit), cancellationToken);
            var premiumRecommendations = recommendations
                .Select(recipe => recipe with { Premium = true })
                .Take(NormalizeLimit(request.Limit))
                .ToList();

            return Ok(new AiRecommendationResponseDto(context.UserId, "Premium", premiumRecommendations));
        }

        var recipes = await _recipeRepository.GetAllAsync(cancellationToken);
        var freeRecommendations = BuildFreeRecommendations(recipes, context, request.Limit);
        return Ok(new AiRecommendationResponseDto(context.UserId, "Free", freeRecommendations));
    }

    [HttpPost("generate-meal-plan")]
    [Authorize(Policy = "RequireActiveSubscription")]
    public async Task<IActionResult> GenerateMealPlan([FromBody] AiMealPlanRequestDto request, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(cancellationToken);
        if (context == null)
        {
            return Unauthorized();
        }

        var mealPlan = await _pythonAiService.GenerateMealPlanAsync(context, NormalizeDays(request.Days), cancellationToken);
        return Ok(mealPlan with { UserId = context.UserId, Mode = "Premium" });
    }

    [HttpPost("coach")]
    [Authorize(Policy = "RequireActiveSubscription")]
    public async Task<IActionResult> Coach([FromBody] AiCoachRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Error = "Message is required." });
        }

        var context = await BuildContextAsync(cancellationToken);
        if (context == null)
        {
            return Unauthorized();
        }

        var coach = await _pythonAiService.AskCoachAsync(context, request.Message, request.Context, cancellationToken);
        return Ok(coach with { UserId = context.UserId, Mode = "Premium" });
    }

    private async Task<AiContextDto?> BuildContextAsync(CancellationToken cancellationToken)
    {
        var user = ControllerContext?.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        var profile = await _userProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        var preferences = await _foodPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        var allergies = await _allergyRepository.GetByUserIdAsync(userId, cancellationToken);

        var allergyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var allergy in allergies)
        {
            if (!string.IsNullOrWhiteSpace(allergy.AllergenName))
            {
                allergyNames.Add(allergy.AllergenName.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(profile?.Allergies))
        {
            foreach (var name in profile.Allergies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                allergyNames.Add(name);
            }
        }

        return new AiContextDto(
            userId,
            profile?.Objective,
            allergyNames.ToList(),
            preferences?.DislikedIngredients?.Where(ingredient => !string.IsNullOrWhiteSpace(ingredient)).Select(ingredient => ingredient.Trim()).ToList() ?? []);
    }

    private SubscriptionSnapshot GetSubscriptionState()
    {
        var user = ControllerContext?.HttpContext?.User;
        var plan = user?.FindFirstValue("subscription_plan");
        var status = user?.FindFirstValue("subscription_status");
        var expiresAtValue = user?.FindFirstValue("subscription_expires_at");

        var isPremium = string.Equals(plan, SubscriptionPlan.Premium.ToString(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(status, SubscriptionStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase)
            && DateTime.TryParse(expiresAtValue, out var expiresAt)
            && expiresAt > DateTime.UtcNow;

        return new SubscriptionSnapshot(isPremium);
    }

    private static int NormalizeLimit(int limit) => limit <= 0 ? 5 : Math.Min(limit, 12);

    private static int NormalizeDays(int days) => days <= 0 ? 7 : Math.Min(days, 14);

    private static IReadOnlyList<AiRecommendationItemDto> BuildFreeRecommendations(IEnumerable<Recipe> recipes, AiContextDto context, int limit)
    {
        var blockedIngredients = new HashSet<string>(context.Allergies.Concat(context.DislikedIngredients), StringComparer.OrdinalIgnoreCase);
        var objective = context.Objective?.Trim().ToLowerInvariant() ?? string.Empty;

        var recommendations = recipes
            .Select(recipe => new
            {
                Recipe = recipe,
                Score = ScoreRecipe(recipe, objective, blockedIngredients, out var reason),
                Reason = reason
            })
            .Where(item => item.Score > int.MinValue)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Recipe.CookingTimeInMinutes)
            .Take(NormalizeLimit(limit))
            .Select(item => new AiRecommendationItemDto(
                item.Recipe.Id.ToString(),
                item.Recipe.Title ?? "Rețetă recomandată",
                item.Recipe.Description,
                item.Recipe.Ingredients
                    .Select(ingredient => ingredient.Ingredient?.Name ?? ingredient.IngredientId.ToString())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList(),
                item.Reason,
                false,
                item.Recipe.CookingTimeInMinutes))
            .ToList();

        if (recommendations.Count > 0)
        {
            return recommendations;
        }

        return recipes
            .OrderBy(recipe => recipe.CookingTimeInMinutes)
            .Take(NormalizeLimit(limit))
            .Select(recipe => new AiRecommendationItemDto(
                recipe.Id.ToString(),
                recipe.Title ?? "Rețetă recomandată",
                recipe.Description,
                recipe.Ingredients
                    .Select(ingredient => ingredient.Ingredient?.Name ?? ingredient.IngredientId.ToString())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList(),
                "Rețetă de bază potrivită pentru utilizatorii Free.",
                false,
                recipe.CookingTimeInMinutes))
            .ToList();
    }

    private static int ScoreRecipe(Recipe recipe, string objective, HashSet<string> blockedIngredients, out string reason)
    {
        var ingredientNames = recipe.Ingredients
            .Select(ingredient => ingredient.Ingredient?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToList();

        if (ingredientNames.Any(name => blockedIngredients.Contains(name)))
        {
            reason = "Rețeta conține un ingredient evitat de utilizator.";
            return int.MinValue;
        }

        if (recipe.Ingredients.Any(ingredient => ingredient.Ingredient != null && objective.Contains("vegan") && !ingredient.Ingredient.IsVegan))
        {
            reason = "Rețeta nu respectă preferința vegană.";
            return int.MinValue;
        }

        var score = 0;
        var reasons = new List<string>();

        if (objective.Contains("weight") || objective.Contains("slab") || objective.Contains("slăb"))
        {
            if (recipe.TotalCalories <= 600)
            {
                score += 3;
                reasons.Add("aport caloric potrivit pentru obiectivul de slăbire");
            }

            if (recipe.CookingTimeInMinutes <= 30)
            {
                score += 1;
                reasons.Add("este rapid de preparat");
            }
        }

        if (objective.Contains("muscle") || objective.Contains("masa") || objective.Contains("masă"))
        {
            if (recipe.TotalProtein >= 20)
            {
                score += 3;
                reasons.Add("conține proteine suficiente");
            }
        }

        if (score == 0)
        {
            score = 1;
            reasons.Add("se potrivește recomandărilor generale");
        }

        reason = string.Join(", ", reasons.Distinct());
        return score;
    }

    private sealed record SubscriptionSnapshot(bool IsPremium);
}
