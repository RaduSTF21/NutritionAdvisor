using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.MealPlans.Queries.GetMealPlans;

public class GetMealPlansQueryHandler : IRequestHandler<GetMealPlansQuery, IEnumerable<MealPlan>>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public GetMealPlansQueryHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<IEnumerable<MealPlan>> Handle(GetMealPlansQuery request, CancellationToken cancellationToken)
    {
        return await _mealPlanRepository.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}