using System.ComponentModel.DataAnnotations;
using NutritionAdvisor.Domain.Enums;
namespace NutritionAdvisor.Domain.Entities;

public class Recipe
{   [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

 
    public string? Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageURL { get; set; }

    public int CookingTimeInMinutes { get; set; }

    public Difficulty Level { get; set; }

    // Relația cu tabelul de legătură 
    public List<RecipeIngredient> Ingredients { get; set; } = new();

    // Instrucțiuni de preparare 
    public string Instructions { get; set; } = string.Empty;

    // Etichete pentru AI (ex: "Keto", "Vegan", "Cina") 🏷
    public List<string> Tags { get; set; } = new();

    // --- PROPRIETĂȚI CALCULATE PENTRU AI ---
    // Acestea calculează automat totalul pe baza ingredientelor adăugate

    public float TotalCalories => Ingredients.Sum(i => (i.Ingredient.Calories * i.Amount) / 100);
    
    public float TotalProtein => Ingredients.Sum(i => (i.Ingredient.Protein * i.Amount) / 100);
    
    public float TotalCarbs => Ingredients.Sum(i => (i.Ingredient.Carbohydrates * i.Amount) / 100);
    
    public float TotalFats => Ingredients.Sum(i => (i.Ingredient.Fats * i.Amount) / 100);

    public float TotalFiber => Ingredients.Sum(i => (i.Ingredient.Fiber * i.Amount) / 100);
}