using MediatR;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandHandler : IRequestHandler<DeleteRecipeCommand, bool>
{
    private readonly IRecipeRepository _repository;

    public DeleteRecipeCommandHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        await _repository.DeleteAsync(request.Id, cancellationToken);
        return true;
    }
}
