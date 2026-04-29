using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;
using NutritionAdvisor.Application.UserProfiles.Queries.GetUserProfile;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Tests.Api;

public class UserProfileControllerTests
{
    [Fact]
    public async Task SaveProfile_ReturnsOk_WhenAuthenticatedUserMatches()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<SaveUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var userId = Guid.NewGuid();
        var controller = CreateController(mediator.Object, userId);

        var command = new SaveUserProfileCommand
        {
            UserId = userId,
            Name = "Radu",
            Gender = "Male",
            Objective = "Weight Loss",
            Age = 27,
            Height = 180,
            Weight = 80,
            Allergies = null,
        };

        var result = await controller.SaveProfile(command);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SaveProfile_ReturnsForbid_WhenUserIdMismatch()
    {
        var mediator = new Mock<IMediator>();
        var controller = CreateController(mediator.Object, Guid.NewGuid());

        var result = await controller.SaveProfile(new SaveUserProfileCommand
        {
            UserId = Guid.NewGuid(),
            Name = "Radu",
            Objective = "Weight Loss"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetProfile_ReturnsUnauthorized_WhenClaimIsInvalid()
    {
        var mediator = new Mock<IMediator>();
        var controller = new UserProfileController(mediator.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
                }, "Bearer"))
            }
        };

        var result = await controller.GetProfile(Guid.NewGuid());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WhenAuthenticatedUserMatches()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                UserId = Guid.NewGuid(),
                Name = "Radu",
                Gender = "Male",
                Age = 27,
                Height = 180,
                Weight = 80,
                Objective = "Weight Loss"
            });

        var userId = Guid.NewGuid();
        var controller = CreateController(mediator.Object, userId);

        var result = await controller.GetProfile(userId);

        Assert.IsType<OkObjectResult>(result);
    }

    private static UserProfileController CreateController(IMediator mediator, Guid userId)
    {
        var controller = new UserProfileController(mediator)
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

        return controller;
    }
}
