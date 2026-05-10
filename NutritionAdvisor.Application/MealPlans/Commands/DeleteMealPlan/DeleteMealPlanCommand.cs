using MediatR;

namespace NutritionAdvisor.Application.MealPlans.Commands.DeleteMealPlan;

public class DeleteMealPlanCommand : IRequest<bool>
{
    public Guid MealPlanId { get; set; }
    public Guid UserId { get; set; }
}