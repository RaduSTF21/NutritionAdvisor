using MediatR;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.MealPlans.Commands.DeleteMealPlan;

public class DeleteMealPlanCommandHandler : IRequestHandler<DeleteMealPlanCommand, bool>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public DeleteMealPlanCommandHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<bool> Handle(DeleteMealPlanCommand request, CancellationToken cancellationToken)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(request.MealPlanId, cancellationToken);
        if (mealPlan == null)
        {
            return false;
        }

        if (mealPlan.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this meal plan.");
        }

        await _mealPlanRepository.DeleteAsync(request.MealPlanId, cancellationToken);
        return true;
    }
}