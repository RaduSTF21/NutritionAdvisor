using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.MealPlans.Queries.GetMealPlanByDate;

public class GetMealPlanByDateQuery : IRequest<MealPlan?>
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
}