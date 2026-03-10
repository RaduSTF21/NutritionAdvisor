using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Queries.GetAllRecipes;

public class GetAllRecipesQueryHandler : IRequestHandler<GetAllRecipesQuery, IEnumerable<Recipe>>
{
    private readonly IRecipeRepository _repository;

    public GetAllRecipesQueryHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Recipe>> Handle(GetAllRecipesQuery request, CancellationToken cancellationToken)
    {
        // The repository returns recipes with their related ingredients included.
        return await _repository.GetAllAsync(cancellationToken);
    }
}