using NutritionAdvisor.Application.AI;

namespace NutritionAdvisor.Application.Interfaces;

public interface IPythonAiService
{
    Task<AiRecommendationResponseModel?> GetRecommendationsAsync(AiRecommendationRequestModel request);
    Task<AiMealPlanResponseModel?> GenerateMealPlanAsync(AiMealPlanRequestModel request);
    Task<AiCoachResponseModel?> GetCoachAdviceAsync(AiCoachRequestModel request);
    Task PrewarmAsync(AiMealPlanRequestModel request);
}
