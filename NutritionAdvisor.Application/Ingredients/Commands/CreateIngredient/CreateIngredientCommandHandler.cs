using MediatR;
using NutritionAdvisor.Application.Ingredients.Commands.CreateIngredient;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Ingredients.Commands.CreateIngredient;

public class CreateIngredientCommandHandler : IRequestHandler<CreateIngredientCommand, Guid>
{
    private readonly IIngredientRepository _repository;

    public CreateIngredientCommandHandler(IIngredientRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateIngredientCommand request, CancellationToken cancellationToken)
    {
        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Calories = request.Calories,
            Protein = request.Protein,
            Carbohydrates = request.Carbohydrates,
            Fats = request.Fats,
            Fiber = request.Fiber,
            Sugar = request.Sugar,
            Sodium = request.Sodium,
            IsGlutenFree = request.IsGlutenFree,
            IsDairyFree = request.IsDairyFree,
            IsVegan = request.IsVegan,
            IsNutFree = request.IsNutFree
        };

        await _repository.AddAsync(ingredient, cancellationToken);
        return ingredient.Id;
    }
}