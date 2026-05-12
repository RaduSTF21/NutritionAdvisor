using MediatR;
using Microsoft.Extensions.Logging;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.MealPlans.Commands.DeleteMealPlan;

public class DeleteMealPlanCommandHandler : IRequestHandler<DeleteMealPlanCommand, bool>
{
    private readonly IMealPlanRepository _repository;
    private readonly ILogger<DeleteMealPlanCommandHandler> _logger;

    public DeleteMealPlanCommandHandler(IMealPlanRepository repository, ILogger<DeleteMealPlanCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteMealPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.MealPlanId, cancellationToken);

        if (plan == null)
        {
            _logger.LogWarning("Delete failed: Meal plan {PlanId} not found.", request.MealPlanId);
            return false;
        }

        if (plan.UserId != request.UserId)
        {
            _logger.LogWarning("Delete forbidden: User {UserId} attempted to delete meal plan {PlanId} belonging to another user.", request.UserId, request.MealPlanId);
            return false;
        }

        await _repository.DeleteAsync(request.MealPlanId, cancellationToken);
        _logger.LogInformation("Deleted meal plan {PlanId} successfully.", request.MealPlanId);

        return true;
    }
}