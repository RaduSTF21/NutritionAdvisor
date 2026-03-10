namespace Frontend.Models;

public class RecipeModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CookingTimeInMinutes { get; set; }
    public string Level { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    
    // Calculated properties populated by the backend.
    public float TotalCalories { get; set; }
    public float TotalProtein { get; set; }
    public float TotalCarbs { get; set; }
    public float TotalFats { get; set; }
}