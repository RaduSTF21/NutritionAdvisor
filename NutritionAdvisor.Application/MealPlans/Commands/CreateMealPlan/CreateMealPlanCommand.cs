using MediatR;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;

public class CreateMealPlanCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public int TargetCalories { get; set; }
    public bool IsAIGenerated { get; set; }
    public bool ReplaceExisting { get; set; }
    public List<CreateMealPlanItemDto> Items { get; set; } = new();
}

public class CreateMealPlanItemDto
{
    public MealType MealType { get; set; }
    public Guid RecipeId { get; set; }
}