using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;
using NutritionAdvisor.Application.MealPlans.Commands.CreateWeeklyMealPlan;
using NutritionAdvisor.Application.MealPlans.Commands.DeleteMealPlan;
using NutritionAdvisor.Application.MealPlans.Queries.GetMealPlanByDate;
using NutritionAdvisor.Application.MealPlans.Queries.GetMealPlans;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Tests.Api;

public class MealPlansControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        var plans = new[]
        {
            new MealPlan
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Date = DateTime.UtcNow.Date,
                Items = new List<MealPlanItem>()
            }
        };

        mediator.Setup(m => m.Send(It.IsAny<GetMealPlansQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var controller = new MealPlansController(mediator.Object);

        var result = await controller.GetAll();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsUnauthorized_WhenNoUserIsPresent()
    {
        var mediator = new Mock<IMediator>();
        var controller = new MealPlansController(mediator.Object);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateDaily_ReturnsUnauthorized_WhenNoUserIsPresent()
    {
        var mediator = new Mock<IMediator>();
        var controller = new MealPlansController(mediator.Object);

        var result = await controller.CreateDaily(new CreateMealPlanCommand
        {
            Date = DateTime.UtcNow,
            TargetCalories = 2000,
            Items = new List<CreateMealPlanItemDto>
            {
                new() { MealType = MealType.Breakfast, RecipeId = Guid.NewGuid() }
            }
        });

        Assert.IsType<UnauthorizedResult>(result);
    }
}