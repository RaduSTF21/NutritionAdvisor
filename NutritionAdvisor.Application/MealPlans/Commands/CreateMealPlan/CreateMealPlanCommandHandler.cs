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

        await HandleExistingMealPlanAsync(request, normalizedDate, cancellationToken);

        var mealPlan = new MealPlan
        {
            UserId = request.UserId,
            Date = normalizedDate,
            TargetCalories = request.TargetCalories,
            IsAIGenerated = request.IsAIGenerated
        };

        foreach (var item in request.Items)
        {
            var planItem = await BuildMealPlanItemAsync(item, cancellationToken);
            mealPlan.Items.Add(planItem);
            mealPlan.TotalCalories += (int)Math.Round(planItem.Calories);
        }

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        return mealPlan.Id;
    }

    // --- METODE PRIVATE PENTRU REDUCEREA COMPLEXITĂȚII COGNITIVE ---

    private async Task HandleExistingMealPlanAsync(CreateMealPlanCommand request, DateTime normalizedDate, CancellationToken cancellationToken)
    {
        var existing = await _mealPlanRepository.GetByUserAndDateAsync(request.UserId, normalizedDate, cancellationToken);

        if (existing != null && !request.ReplaceExisting)
        {
            throw new InvalidOperationException("A meal plan already exists for that date.");
        }

        if (existing != null)
        {
            await _mealPlanRepository.DeleteAsync(existing.Id, cancellationToken);
        }
    }

    private async Task<MealPlanItem> BuildMealPlanItemAsync(CreateMealPlanItemDto item, CancellationToken cancellationToken)
    {
        var planItem = new MealPlanItem
        {
            MealType = item.MealType,
            Calories = item.Calories,
            Protein = item.Protein,
            Carbs = item.Carbs,
            Fats = item.Fats
        };

        if (item.RecipeId.HasValue && item.RecipeId != Guid.Empty)
        {
            var recipe = await _recipeRepository.GetByIdAsync(item.RecipeId.Value, cancellationToken);
            if (recipe != null)
            {
                planItem.RecipeId = recipe.Id;
                planItem.Recipe = recipe;
                if (Math.Abs(planItem.Calories) < 0.0001) planItem.Calories = recipe.TotalCalories;
                return planItem;
            }
        }

        // Fallback dacă rețeta nu a fost găsită în DB sau dacă e doar un link extern
        planItem.ExternalTitle = item.ExternalTitle ?? item.Title ?? "External recipe";
        planItem.ExternalUrl = item.ExternalUrl;

        return planItem;
    }
}