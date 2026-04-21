using MediatR;

namespace NutritionAdvisor.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public DeleteRecipeCommand(Guid id)
    {
        Id = id;
    }
}
