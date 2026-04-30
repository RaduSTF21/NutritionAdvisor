using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using NutritionAdvisor.Application.Recipes.Commands.CreateRecipe;
using NutritionAdvisor.Application.Recipes.Commands.DeleteRecipe;
using NutritionAdvisor.Application.Recipes.Commands.UpdateRecipe;
using NutritionAdvisor.Application.Recipes.Queries.GetAllRecipes;
using NutritionAdvisor.Application.Recipes.Queries.GetRecipesById;
using NutritionAdvisor.Domain.Enums;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Tests.Api;

public class RecipesControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        var recipes = new[]
        {
            new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Salad",
                Instructions = "Mix",
                Level = Difficulty.Easy
            }
        };
        mediator.Setup(m => m.Send(It.IsAny<GetAllRecipesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipes);

        var controller = new RecipesController(mediator.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(recipes, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsRecipeId()
    {
        var mediator = new Mock<IMediator>();
        var recipeId = Guid.NewGuid();
        mediator.Setup(m => m.Send(It.IsAny<CreateRecipeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipeId);

        var controller = new RecipesController(mediator.Object);

        var result = await controller.Create(new CreateRecipeCommand());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(recipeId, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenRecipeIsMissing()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetRecipeByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recipe?)null);

        var controller = new RecipesController(mediator.Object);

        var result = await controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenUpdateSucceeds()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<UpdateRecipeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new RecipesController(mediator.Object);
        var command = new UpdateRecipeCommand();

        var result = await controller.Update(Guid.NewGuid(), command);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenDeleteFails()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<DeleteRecipeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = new RecipesController(mediator.Object);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }
}
