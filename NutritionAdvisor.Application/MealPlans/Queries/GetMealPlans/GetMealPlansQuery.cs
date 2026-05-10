using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.MealPlans.Queries.GetMealPlans;

public class GetMealPlansQuery : IRequest<IEnumerable<MealPlan>>
{
    public Guid UserId { get; set; }
}