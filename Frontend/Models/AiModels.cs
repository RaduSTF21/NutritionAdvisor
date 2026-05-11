using System.Text.Json.Serialization;

namespace Frontend.Models;

public sealed record AiRecommendationRequestModel(int Limit = 5, string? SearchQuery = null, bool UseInternetSearch = false);
public sealed record AiMealPlanRequestModel(int Days = 7);
public sealed record AiCoachRequestModel(string Message, string? Context = null);

public sealed record AiRecommendationItemModel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("ingredients")] IReadOnlyList<string> Ingredients,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("premium")] bool Premium,
    [property: JsonPropertyName("cooking_time_in_minutes")] int CookingTimeInMinutes,
    [property: JsonPropertyName("externalUrl")] string? ExternalUrl = null);

public sealed record AiRecommendationResponseModel(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("recommendations")] IReadOnlyList<AiRecommendationItemModel> Recommendations);

public sealed record AiMealPlanDayModel(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("calories")] int Calories,
    [property: JsonPropertyName("items")] IReadOnlyList<AiMealPlanMealModel> Items);

public sealed record AiMealPlanMealModel(
    [property: JsonPropertyName("mealType")] string MealType,
    [property: JsonPropertyName("recipeId")] Guid? RecipeId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("externalUrl")] string? ExternalUrl,
    [property: JsonPropertyName("calories")] int Calories,
    [property: JsonPropertyName("protein")] int Protein,
    [property: JsonPropertyName("carbs")] int Carbs,
    [property: JsonPropertyName("fats")] int Fats);

public sealed record AiMealPlanResponseModel(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("days")] IReadOnlyList<AiMealPlanDayModel> Days);

public sealed record AiCoachResponseModel(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("tips")] IReadOnlyList<string> Tips);
