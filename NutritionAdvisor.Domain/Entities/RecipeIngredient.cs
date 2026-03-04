namespace NutritionAdvisor.Domain.Entities;

public class RecipeIngredient
{
    // Legătura către Rețetă 🍲
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!; 

    // Legătura către Ingredient 🍎
    public Guid IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!; 

    // Date specifice (Payload) ⚖️
    public float Amount { get; set; } 
    public string Unit { get; set; } = "g";
    
}