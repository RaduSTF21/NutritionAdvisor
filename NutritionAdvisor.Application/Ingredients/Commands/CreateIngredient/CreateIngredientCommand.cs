using MediatR;

namespace NutritionAdvisor.Application.Ingredients.Commands.CreateIngredient;

public class CreateIngredientCommand : IRequest<Guid>
{
    public required string Name { get; set; }
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fats { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
    
    public bool IsGlutenFree { get; set; }
    public bool IsDairyFree { get; set; }
    public bool IsVegan { get; set; }
    public bool IsNutFree { get; set; }
}