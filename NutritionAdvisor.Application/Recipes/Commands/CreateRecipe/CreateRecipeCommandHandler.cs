using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandler : IRequestHandler<CreateRecipeCommand, Guid>
{
    private readonly IRecipeRepository _recipeRepository;
    private readonly IFileStorageService _fileStorageService;
    public CreateRecipeCommandHandler(IRecipeRepository recipeRepository, IFileStorageService fileStorageService)
    {
        _recipeRepository = recipeRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Guid> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        string finalImageUrl = string.Empty;

        if(request.ImageFileData != null && !string.IsNullOrEmpty(request.ImageFileName))
        {
            finalImageUrl = await _fileStorageService.UploadFileAsync(request.ImageFileData, request.ImageFileName, cancellationToken);
        }
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ImageURL = finalImageUrl,
            CookingTimeInMinutes = request.CookingTimeInMinutes,
            Level = request.Level,
            Instructions = request.Instructions,
            Tags = request.Tags
        };

        recipe.Ingredients = request.Ingredients.Select(i => new RecipeIngredient
        {
            RecipeId = recipe.Id,
            IngredientId = i.IngredientId,
            Amount = i.Amount,
            Unit = i.Unit
        }).ToList();

        await _recipeRepository.AddAsync(recipe, cancellationToken);
        return recipe.Id;
    }
}