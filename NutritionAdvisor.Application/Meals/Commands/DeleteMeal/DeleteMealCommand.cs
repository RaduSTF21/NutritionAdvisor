using MediatR;

namespace NutritionAdvisor.Application.Meals.Commands.DeleteMeal;

public class DeleteMealCommand : IRequest
{
    public Guid MealId { get; set; }
    public Guid UserId { get; set; }

    public DeleteMealCommand(Guid mealId, Guid userId)
    {
        MealId = mealId;
        UserId = userId;
    }
}