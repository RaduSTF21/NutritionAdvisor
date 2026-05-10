namespace NutritionAdvisor.Application.AI;

public record AiRecommendationRequestModel(string UserId, string? Objective, List<string> Allergies, List<string> DislikedIngredients, int Limit, List<AiRecipeDto> AvailableRecipes, string? SearchQuery = null, bool UseInternetSearch = false);
public record AiMealPlanRequestModel(
    string UserId,
    string? Objective,
    int Days,
    List<string> Allergies,
    List<string> DislikedIngredients,
    List<AiRecipeDto> AvailableRecipes,
    double? WeightKg = null,
    double? HeightCm = null,
    int? Age = null,
    string? Gender = null,
    string? DietType = null,
    List<string>? PreferredCuisines = null);
public record AiCoachRequestModel(string UserId, string? Objective, string Message, string? Context, List<string> Allergies, List<string> DislikedIngredients);
public record AiRecipeDto(Guid Id, string Title, string Description, List<string> Ingredients, int Calories, int CookingTimeInMinutes, string Goal);

public record AiRecommendationResponseModel(Guid UserId, string Mode, List<AiRecommendationItemModel> Recommendations);
public record AiRecommendationItemModel(string Id, string Title, string? Description, List<string> Ingredients, string Reason, bool Premium, int CookingTimeInMinutes, string? ExternalUrl = null);

public record AiMealPlanResponseModel(Guid UserId, string Mode, string Summary, List<AiMealPlanDayModel> Days);
public record AiMealPlanDayModel(string Date, string Title, string Description, int Calories, List<AiMealPlanMealModel> Items);
public record AiMealPlanMealModel(string MealType, Guid? RecipeId, string Title, string? ExternalUrl, int Calories, int Protein, int Carbs, int Fats);

public record AiCoachResponseModel(Guid UserId, string Mode, string Answer, List<string> Tips);