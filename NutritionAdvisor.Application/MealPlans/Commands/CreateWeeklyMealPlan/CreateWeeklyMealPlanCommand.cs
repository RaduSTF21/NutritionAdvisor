using MediatR;
using NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;

namespace NutritionAdvisor.Application.MealPlans.Commands.CreateWeeklyMealPlan;

public class CreateWeeklyMealPlanCommand : IRequest<List<Guid>>
{
    public Guid UserId { get; set; }
    public List<DailyMealPlanRequest> DailyPlans { get; set; } = new();
}

public class DailyMealPlanRequest
{
    public DateTime Date { get; set; }
    public int TargetCalories { get; set; }
    public bool IsAIGenerated { get; set; }
    public List<CreateMealPlanItemDto> Items { get; set; } = new();
}