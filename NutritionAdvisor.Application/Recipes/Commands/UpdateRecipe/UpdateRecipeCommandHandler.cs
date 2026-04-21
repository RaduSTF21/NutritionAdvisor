using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandHandler : IRequestHandler<UpdateRecipeCommand, bool>
{
    private readonly IRecipeRepository _recipeRepository;

    public UpdateRecipeCommandHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<bool> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (recipe == null)
        {
            return false;
        }

        recipe.Title = request.Title;
        recipe.Description = request.Description;
        recipe.CookingTimeInMinutes = request.CookingTimeInMinutes;
        recipe.Level = request.Level;
        recipe.Instructions = request.Instructions;
        recipe.Tags = request.Tags;

        recipe.Ingredients.Clear();
        foreach (var ingredient in request.Ingredients)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                IngredientId = ingredient.IngredientId,
                Amount = ingredient.Amount,
                Unit = ingredient.Unit
            });
        }

        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        return true;
    }
}
