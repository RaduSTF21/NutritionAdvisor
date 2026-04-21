using MediatR;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Application.Recipes.Queries.GetRecipesByFilter;

public class GetRecipesByFilterQuery : IRequest<IEnumerable<Recipe>>
{
    public string? SearchTerm { get; set; }
    public string? Tag { get; set; }
    public Difficulty? Level { get; set; }
}
