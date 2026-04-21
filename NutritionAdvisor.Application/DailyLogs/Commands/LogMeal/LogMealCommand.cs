using MediatR;

namespace NutritionAdvisor.Application.DailyLogs.Commands.LogMeal;

public record LogMealCommand(Guid UserId, Guid RecipeId) : IRequest<Guid>;