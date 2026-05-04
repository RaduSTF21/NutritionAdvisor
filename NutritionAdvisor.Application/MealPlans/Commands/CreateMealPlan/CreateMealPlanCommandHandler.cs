using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;

public class CreateMealPlanCommandHandler : IRequestHandler<CreateMealPlanCommand, Guid>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IRecipeRepository _recipeRepository;

    public CreateMealPlanCommandHandler(IMealPlanRepository mealPlanRepository, IRecipeRepository recipeRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _recipeRepository = recipeRepository;
    }

    public async Task<Guid> Handle(CreateMealPlanCommand request, CancellationToken cancellationToken)
    {
        var normalizedDate = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        var existing = await _mealPlanRepository.GetByUserAndDateAsync(request.UserId, normalizedDate, cancellationToken);
        if (existing != null && !request.ReplaceExisting)
        {
            throw new InvalidOperationException("A meal plan already exists for that date.");
        }

        if (existing != null)
        {
            await _mealPlanRepository.DeleteAsync(existing.Id, cancellationToken);
        }

        var mealPlan = new MealPlan
        {
            UserId = request.UserId,
            Date = normalizedDate,
            TargetCalories = request.TargetCalories,
            IsAIGenerated = request.IsAIGenerated
        };

        foreach (var item in request.Items)
        {
            var recipe = await _recipeRepository.GetByIdAsync(item.RecipeId, cancellationToken);
            if (recipe == null)
            {
                throw new InvalidOperationException($"Recipe {item.RecipeId} was not found.");
            }

            mealPlan.Items.Add(new MealPlanItem
            {
                MealType = item.MealType,
                RecipeId = recipe.Id,
                Recipe = recipe
            });

            mealPlan.TotalCalories += (int)Math.Round(recipe.TotalCalories);
        }

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        return mealPlan.Id;
    }
}