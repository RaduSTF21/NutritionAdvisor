using MediatR;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CookingTimeInMinutes { get; set; }
    public Difficulty Level { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<UpdateRecipeIngredientDto> Ingredients { get; set; } = new();
}

public class UpdateRecipeIngredientDto
{
    public Guid IngredientId { get; set; }
    public float Amount { get; set; }
    public string Unit { get; set; } = "g";
}
