namespace NutritionAdvisor.Domain.Entities;

public class Ingredient
{
    public Guid Id { get; init; }
    public required string Name { get; set; }

    // Valori per 100g/ml
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fats { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }

    // Indicatori pentru AI (Alergeni & Diete)
    public bool IsGlutenFree { get; set; }
    public bool IsDairyFree { get; set; }
    public bool IsVegan { get; set; }
    public bool IsNutFree { get; set; }
}