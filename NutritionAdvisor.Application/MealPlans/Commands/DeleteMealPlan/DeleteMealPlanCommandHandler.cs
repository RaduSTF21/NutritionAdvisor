using MediatR;
using Microsoft.Extensions.Logging;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.MealPlans.Commands.DeleteMealPlan;

public class DeleteMealPlanCommandHandler : IRequestHandler<DeleteMealPlanCommand, bool>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly ILogger<DeleteMealPlanCommandHandler> _logger;

    public DeleteMealPlanCommandHandler(IMealPlanRepository mealPlanRepository, ILogger<DeleteMealPlanCommandHandler> logger)
    {
        _mealPlanRepository = mealPlanRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteMealPlanCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteMealPlan called. MealPlanId={MealPlanId} UserId={UserId}", request.MealPlanId, request.UserId);
        var mealPlan = await _mealPlanRepository.GetByIdAsync(request.MealPlanId, cancellationToken);
        if (mealPlan == null)
        {
            _logger.LogWarning("MealPlan not found: {MealPlanId}", request.MealPlanId);
            return false;
        }

        if (mealPlan.UserId != request.UserId)
        {
            _logger.LogWarning("Unauthorized delete attempt. MealPlanId={MealPlanId} Owner={OwnerId} RequestUser={UserId}", request.MealPlanId, mealPlan.UserId, request.UserId);
            throw new UnauthorizedAccessException("You are not allowed to delete this meal plan.");
        }

        await _mealPlanRepository.DeleteAsync(request.MealPlanId, cancellationToken);
        _logger.LogInformation("MealPlan deleted: {MealPlanId}", request.MealPlanId);
        return true;
    }
}