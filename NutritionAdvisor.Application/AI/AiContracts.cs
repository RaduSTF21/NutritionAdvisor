using System.Text.Json.Serialization;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.AI;

public sealed record AiRecommendationRequestDto
{
    public int Limit { get; init; } = 5;
}

public sealed record AiMealPlanRequestDto
{
    public int Days { get; init; } = 7;
}

public sealed record AiCoachRequestDto
{
    public string Message { get; init; } = string.Empty;

    public string? Context { get; init; }
}

public sealed record AiContextDto(
    [property: JsonPropertyName("user_id")] Guid UserId,
    [property: JsonPropertyName("objective")] string? Objective,
    [property: JsonPropertyName("allergies")] IReadOnlyList<string> Allergies,
    [property: JsonPropertyName("disliked_ingredients")] IReadOnlyList<string> DislikedIngredients);

public sealed record AiRecommendationItemDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("ingredients")] IReadOnlyList<string> Ingredients,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("premium")] bool Premium,
    [property: JsonPropertyName("cooking_time_in_minutes")] int CookingTimeInMinutes);

public sealed record AiRecommendationResponseDto(
    Guid UserId,
    string Mode,
    IReadOnlyList<AiRecommendationItemDto> Recommendations);

public sealed record AiMealPlanDayDto(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("calories")] int Calories);

public sealed record AiMealPlanResponseDto(
    Guid UserId,
    string Mode,
    string Summary,
    IReadOnlyList<AiMealPlanDayDto> Days);

public sealed record AiCoachResponseDto(
    Guid UserId,
    string Mode,
    string Answer,
    IReadOnlyList<string> Tips);
