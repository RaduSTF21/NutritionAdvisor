using System;
using System.Collections.Generic;
using System.Linq;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;
using Xunit;

namespace NutritionAdvisor.Tests.Domain;

public class EntitiesTests
{
    [Fact]
    public void Allergy_AllProperties_AreSetAndRetrievedCorrectly()
    {
        // Arrange
        var allergyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var allergy = new Allergy
        {
            Id = allergyId,
            UserId = userId,
            AllergenName = "Lactose",
            SeverityLevel = AllergySeverity.Severe
        };

        // Assert
        Assert.Equal(allergyId, allergy.Id);
        Assert.Equal(userId, allergy.UserId);
        Assert.Equal("Lactose", allergy.AllergenName);
        Assert.Equal(AllergySeverity.Severe, allergy.SeverityLevel);
    }

    [Fact]
    public void Recipe_AllProperties_AreSetAndRetrievedCorrectly()
    {
        // Arrange
        var recipeId = Guid.NewGuid();

        // Act
        var recipe = new Recipe
        {
            Id = recipeId,
            Title = "Avocado Toast",
            Description = "Healthy and quick breakfast",
            Instructions = "1. Toast bread. 2. Mash avocado. 3. Put avocado on bread.",
            ImageURL = "https://example.com/avocado.png",
            CookingTimeInMinutes = 5,
            Level = Difficulty.Easy,
            Ingredients = new List<RecipeIngredient>(),
            Meals = new List<Meal>()
        };

        // Assert (verificăm getterele directe)
        Assert.Equal(recipeId, recipe.Id);
        Assert.Equal("Avocado Toast", recipe.Title);
        Assert.Equal("Healthy and quick breakfast", recipe.Description);
        Assert.Equal("1. Toast bread. 2. Mash avocado. 3. Put avocado on bread.", recipe.Instructions);
        Assert.Equal("https://example.com/avocado.png", recipe.ImageURL);
        Assert.Equal(5, recipe.CookingTimeInMinutes);
        Assert.Equal(Difficulty.Easy, recipe.Level);

        // Verificăm colecțiile dacă sunt instanțiate corect
        Assert.Empty(recipe.Ingredients);
        Assert.Empty(recipe.Meals);

        // Verificăm proprietățile Read-Only (Care ar trebui să fie 0 atâta timp cât lista de Ingredients e goală)
        Assert.Equal(0, recipe.TotalCalories);
        Assert.Equal(0, recipe.TotalProtein);
        Assert.Equal(0, recipe.TotalCarbs);
        Assert.Equal(0, recipe.TotalFats);
    }

    [Fact]
    public void RecipeIngredient_Mapping_WorksCorrectly()
    {
        // Arrange
        var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Smoothie" };
        var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "Banana", Calories = 89 };

        // Act
        var mapping = new RecipeIngredient
        {
            RecipeId = recipe.Id,
            Recipe = recipe,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            Amount = 1.5f,
            Unit = "pieces"
        };

        recipe.Ingredients.Add(mapping);

        // Assert
        Assert.Equal(1.5f, mapping.Amount);
        Assert.Equal("pieces", mapping.Unit);
        Assert.NotNull(mapping.Recipe);
        Assert.Equal("Smoothie", mapping.Recipe.Title);
        Assert.NotNull(mapping.Ingredient);
        Assert.Equal("Banana", mapping.Ingredient.Name);
        Assert.Single(recipe.Ingredients);
    }

    [Fact]
    public void Meal_And_MealPlanItem_RelationToRecipe_WorksCorrectly()
    {
        // Arrange
        var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Salad" };

        // Act - Verificăm entitatea Meal
        var meal = new Meal
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            Recipe = recipe,
            EatenAt = DateTime.UtcNow
        };

        // Act - Verificăm entitatea MealPlanItem
        var planItem = new MealPlanItem
        {
            Id = Guid.NewGuid(),
            MealPlanId = Guid.NewGuid(),
            MealType = MealType.Lunch,
            RecipeId = recipe.Id,
            Recipe = recipe,
            ExternalTitle = "External Salad",
            ExternalUrl = "http://link.com",
            Calories = 100f,
            Protein = 2f,
            Carbs = 10f,
            Fats = 1f
        };

        // Assert
        Assert.Equal(recipe.Id, meal.RecipeId);
        Assert.Equal("Salad", meal.Recipe.Title);

        Assert.Equal(recipe.Id, planItem.RecipeId);
        Assert.Equal("External Salad", planItem.ExternalTitle);
        Assert.Equal("http://link.com", planItem.ExternalUrl);
        Assert.Equal(100f, planItem.Calories);
        Assert.Equal(MealType.Lunch, planItem.MealType);
    }
}