using System.ComponentModel.DataAnnotations;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Domain.Entities;

public class Recipe
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    public string? Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageURL { get; set; }

    public int CookingTimeInMinutes { get; set; }

    public Difficulty Level { get; set; }

    public List<RecipeIngredient> Ingredients { get; set; } = new();

    public string Instructions { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();
    public List<Meal> Meals { get; set; } = new List<Meal>();



    public float TotalCalories => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Calories * i.Amount) / 100 : 0) ?? 0;

    public float TotalProtein => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Protein * i.Amount) / 100 : 0) ?? 0;

    public float TotalCarbs => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Carbohydrates * i.Amount) / 100 : 0) ?? 0;

    public float TotalFats => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Fats * i.Amount) / 100 : 0) ?? 0;

    public float TotalFiber => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Fiber * i.Amount) / 100 : 0) ?? 0;

    public float TotalSugar => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Sugar * i.Amount) / 100 : 0) ?? 0;

    public float TotalSodium => Ingredients?
        .Sum(i => i.Ingredient != null ? (i.Ingredient.Sodium * i.Amount) / 100 : 0) ?? 0;
}