
using Frontend.Models;
public class RecipeIngredientModel
{
    public IngredientModel Ingredient { get; set; } = new IngredientModel();
    public float Amount { get; set; }
    public string Unit { get; set; } = "g";
}