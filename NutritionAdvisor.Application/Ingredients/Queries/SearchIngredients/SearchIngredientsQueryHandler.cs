using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Application.Ingredients.Queries.SearchIngredients;
using NutritionAdvisor.Domain.Entities;


namespace NutritionAdvisor.Application.Ingredients.Queries.SearchIngredients;

public class SearchIngredientsQueryHandler : IRequestHandler<SearchIngredientsQuery, IEnumerable<Ingredient>>
{
    private readonly IIngredientRepository _repository;

    public SearchIngredientsQueryHandler(IIngredientRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Ingredient>> Handle(SearchIngredientsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return await _repository.GetAllAsync(cancellationToken);
        }

        return await _repository.SearchByNameAsync(request.SearchTerm, cancellationToken);
    }
}