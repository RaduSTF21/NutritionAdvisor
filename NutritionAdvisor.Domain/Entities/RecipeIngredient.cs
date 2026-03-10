using System.Text.Json.Serialization;
namespace NutritionAdvisor.Domain.Entities;
public class RecipeIngredient
{
    // Link back to the recipe.
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!; 

    // Link back to the ingredient.
    public Guid IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!; 

    // Payload fields specific to this relationship.
    public float Amount { get; set; } 
    public string Unit { get; set; } = "g";
    
}