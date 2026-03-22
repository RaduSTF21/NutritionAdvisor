using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandler : IRequestHandler<CreateRecipeCommand, Guid>
{
    private readonly IRecipeRepository _recipeRepository;

    public CreateRecipeCommandHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Guid> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ImageURL = request.ImageURL,
            CookingTimeInMinutes = request.CookingTimeInMinutes,
            Level = request.Level,
            Instructions = request.Instructions,
            Tags = request.Tags
        };

        // Map DTOs to the RecipeIngredient join entity.
        recipe.Ingredients = request.Ingredients.Select(i => new RecipeIngredient
        {
            RecipeId = recipe.Id,
            IngredientId = i.IngredientId,
            Amount = i.Amount,
            Unit = i.Unit
        }).ToList();

        await _recipeRepository.AddAsync(recipe, cancellationToken);
        return recipe.Id;
    }
}