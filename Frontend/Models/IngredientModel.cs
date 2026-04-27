public class IngredientModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fats { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
}