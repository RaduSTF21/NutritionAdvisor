namespace NutritionAdvisor.Application.Recipes.Queries.GetRecipesById;
public record GetRecipeByIdQuery(Guid Id) : MediatR.IRequest<NutritionAdvisor.Domain.Entities.Recipe?>;