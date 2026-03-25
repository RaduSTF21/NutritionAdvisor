using MediatR;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommand : IRequest<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageFileName { get; set; }
    public byte[]? ImageFileData { get; set; }
    public int CookingTimeInMinutes { get; set; }
    public Difficulty Level { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();

    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
}

public class RecipeIngredientDto
{
    public Guid IngredientId { get; set; }
    public float Amount { get; set; }
    public string Unit { get; set; } = "g";
}