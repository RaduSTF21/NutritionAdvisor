using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Queries.GetRecipesById;

public class GetRecipeByIdHandler : IRequestHandler<GetRecipeByIdQuery, Recipe?>
{
    private readonly IRecipeRepository _recipeRepository;

    public GetRecipeByIdHandler(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Recipe?> Handle(GetRecipeByIdQuery request, CancellationToken cancellationToken)
    {
        // Retrieve the recipe by ID from the repository.
        var recipe = await _recipeRepository.GetByIdAsync(request.Id,cancellationToken);
        return recipe;
    }
}