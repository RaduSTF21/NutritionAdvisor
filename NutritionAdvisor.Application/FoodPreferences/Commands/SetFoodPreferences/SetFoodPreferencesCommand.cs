using MediatR;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.FoodPreferences.Commands.SetFoodPreferences;

public class SetFoodPreferencesCommand : IRequest<Guid>
{
    public Guid UserId { get; set; }
    public DietType DietType { get; set; } = DietType.None;
    public List<string> PreferredCuisines { get; set; } = new();
    public List<string> DislikedIngredients { get; set; } = new();
}
