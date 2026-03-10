using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Queries.GetAllRecipes;

public class GetAllRecipesQuery : IRequest<IEnumerable<Recipe>>
{
}