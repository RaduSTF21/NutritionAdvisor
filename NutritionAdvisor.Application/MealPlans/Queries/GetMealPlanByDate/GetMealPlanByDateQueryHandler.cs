using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.MealPlans.Queries.GetMealPlanByDate;

public class GetMealPlanByDateQueryHandler : IRequestHandler<GetMealPlanByDateQuery, MealPlan?>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public GetMealPlanByDateQueryHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<MealPlan?> Handle(GetMealPlanByDateQuery request, CancellationToken cancellationToken)
    {
        return await _mealPlanRepository.GetByUserAndDateAsync(request.UserId, request.Date, cancellationToken);
    }
}