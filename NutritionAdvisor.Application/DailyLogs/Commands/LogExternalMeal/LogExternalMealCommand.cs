using MediatR;

namespace NutritionAdvisor.Application.DailyLogs.Commands.LogExternalMeal;

public sealed record LogExternalMealCommand(
    Guid UserId,
    string Title,
    string? ExternalUrl,
    int Calories,
    int Protein,
    int Carbs,
    int Fats) : IRequest<Guid>;
