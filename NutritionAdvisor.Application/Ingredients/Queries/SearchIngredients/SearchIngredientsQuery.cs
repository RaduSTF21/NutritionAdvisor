using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Ingredients.Queries.SearchIngredients;

public class SearchIngredientsQuery : IRequest<IEnumerable<Ingredient>>
{
    public string SearchTerm { get; set; } = string.Empty;
}