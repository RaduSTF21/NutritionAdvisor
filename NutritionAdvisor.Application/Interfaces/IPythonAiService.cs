using NutritionAdvisor.Application.AI;

namespace NutritionAdvisor.Application.Interfaces;

public interface IPythonAiService
{
    Task<IReadOnlyList<AiRecommendationItemDto>> RecommendRecipesAsync(AiContextDto context, int limit, CancellationToken cancellationToken);
    Task<AiMealPlanResponseDto> GenerateMealPlanAsync(AiContextDto context, int days, CancellationToken cancellationToken);
    Task<AiCoachResponseDto> AskCoachAsync(AiContextDto context, string message, string? extraContext, CancellationToken cancellationToken);
}
