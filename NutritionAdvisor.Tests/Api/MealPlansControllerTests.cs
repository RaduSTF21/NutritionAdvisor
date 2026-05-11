using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
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
    public async Task GetAll_ReturnsOk_WhenUserIsAuthenticated()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var mediator = new Mock<IMediator>();
        var plans = new[]
        {
            new MealPlan
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = DateTime.UtcNow.Date,
                Items = new List<MealPlanItem>()
            }
        };

        mediator.Setup(m => m.Send(It.IsAny<GetMealPlansQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plans);

        var controller = CreateController(userId, mediator.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(plans, ok.Value);
        mediator.Verify(m => m.Send(It.Is<GetMealPlansQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByDate_ReturnsBadRequest_ForInvalidDate()
    {
        var controller = CreateController(Guid.NewGuid(), new Mock<IMediator>().Object);

        var result = await controller.GetByDate("not-a-date");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Use the yyyy-MM-dd date format.", badRequest.Value);
    }

    [Fact]
    public async Task GetByDate_ReturnsOk_WhenPlanExists()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var mediator = new Mock<IMediator>();
        var plan = new MealPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = new DateTime(2026, 5, 11),
            Items = new List<MealPlanItem>()
        };

        mediator.Setup(m => m.Send(It.IsAny<GetMealPlanByDateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var controller = CreateController(userId, mediator.Object);

        var result = await controller.GetByDate("2026-05-11");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(plan, ok.Value);
        mediator.Verify(m => m.Send(It.Is<GetMealPlanByDateQuery>(q => q.UserId == userId && q.Date == new DateTime(2026, 5, 11)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWeekly_ReturnsOk_AndSetsUserId()
    {
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var mediator = new Mock<IMediator>();
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        mediator.Setup(m => m.Send(It.IsAny<CreateWeeklyMealPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);

        var controller = CreateController(userId, mediator.Object);

        var command = new CreateWeeklyMealPlanCommand();
        var result = await controller.CreateWeekly(command);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(ids, ok.Value);
        Assert.Equal(userId, command.UserId);
        mediator.Verify(m => m.Send(It.IsAny<CreateWeeklyMealPlanCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenMealPlanIsDeleted()
    {
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<DeleteMealPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(userId, mediator.Object);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
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

    [Fact]
    public async Task CreateDaily_ReturnsOk_AndSetsUserId()
    {
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var mediator = new Mock<IMediator>();
        var createdId = Guid.NewGuid();
        mediator.Setup(m => m.Send(It.IsAny<CreateMealPlanCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdId);

        var controller = CreateController(userId, mediator.Object);
        var command = new CreateMealPlanCommand
        {
            Date = DateTime.UtcNow,
            TargetCalories = 2200,
            Items = new List<CreateMealPlanItemDto>
            {
                new() { MealType = MealType.Breakfast, RecipeId = Guid.NewGuid() }
            }
        };

        var result = await controller.CreateDaily(command);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(createdId, ok.Value);
        Assert.Equal(userId, command.UserId);
        mediator.Verify(m => m.Send(It.IsAny<CreateMealPlanCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static MealPlansController CreateController(Guid userId, IMediator mediator)
    {
        return new MealPlansController(mediator)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "Bearer"))
                }
            }
        };
    }
}