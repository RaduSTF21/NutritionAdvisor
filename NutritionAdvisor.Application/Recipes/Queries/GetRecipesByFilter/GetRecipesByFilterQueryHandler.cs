using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Queries.GetRecipesByFilter;

public class GetRecipesByFilterQueryHandler : IRequestHandler<GetRecipesByFilterQuery, IEnumerable<Recipe>>
{
    private readonly IRecipeRepository _repository;

    public GetRecipesByFilterQueryHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Recipe>> Handle(GetRecipesByFilterQuery request, CancellationToken cancellationToken)
    {
        return await _repository.FilterAsync(request.SearchTerm, request.Tag, request.Level, cancellationToken);
    }
}
