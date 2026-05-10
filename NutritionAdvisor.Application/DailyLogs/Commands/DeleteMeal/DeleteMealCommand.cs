using MediatR;

namespace NutritionAdvisor.Application.DailyLogs.Commands.DeleteMeal;

public record DeleteMealCommand(Guid UserId, Guid MealId) : IRequest<bool>;
