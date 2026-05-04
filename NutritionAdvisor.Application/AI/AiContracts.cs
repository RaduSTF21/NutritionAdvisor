namespace NutritionAdvisor.Application.AI;

// DTO pentru a trimite rețetele noastre din DB către Python
public record AiRecipeDto(
    Guid Id,
    string Title,
    string Description,
    List<string> Ingredients,
    int Calories,
    int CookingTimeInMinutes,
    string Goal);

// Request-urile către Python
public record AiRecommendationRequestModel(
    string UserId,
    string? Objective,
    List<string> Allergies,
    List<string> DislikedIngredients,
    int Limit,
    List<AiRecipeDto> AvailableRecipes);

public record AiMealPlanRequestModel(
    string UserId,
    string? Objective,
    int Days,
    List<string> Allergies,
    List<string> DislikedIngredients,
    List<AiRecipeDto> AvailableRecipes);

public record AiCoachRequestModel(
    string UserId,
    string? Objective,
    string Message,
    string? Context,
    List<string> Allergies,
    List<string> DislikedIngredients);

// Modelele de răspuns (rămân la fel pentru a fi compatibile cu Blazor)
public record AiRecommendationResponseModel(string UserId, string Mode, List<AiRecommendationItemModel> Recommendations);
public record AiRecommendationItemModel(string Id, string Title, string Description, List<string> Ingredients, string Reason, bool Premium, int CookingTimeInMinutes);

public record AiMealPlanResponseModel(string UserId, string Mode, string Summary, List<AiMealPlanDayModel> Days);
public record AiMealPlanDayModel(string Date, string Title, string Description, int Calories, List<AiMealPlanMealModel> Items);
public record AiMealPlanMealModel(string MealType, Guid RecipeId, string Title, int Calories);

public record AiCoachResponseModel(string UserId, string Mode, string Answer, List<string> Tips);