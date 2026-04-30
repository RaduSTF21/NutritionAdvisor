using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NutritionAdvisor.API.Controllers;
using NutritionAdvisor.Application.Users.Commands.LoginUser;
using NutritionAdvisor.Application.Users.Commands.RegisterUser;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Tests.Api;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsUserId_WhenMediatorSucceeds()
    {
        var mediator = new Mock<IMediator>();
        var userId = Guid.NewGuid();
        mediator.Setup(m => m.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var controller = new AuthController(mediator.Object, BuildConfiguration());

        var result = await controller.Register(new RegisterUserCommand("Radu", "radu@example.com", "Secret123!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedUserId = GetPropertyValue<Guid>(ok.Value!, "UserId");
        Assert.Equal(userId, returnedUserId);
    }

    [Fact]
    public async Task Login_ReturnsJwtToken_WithSubscriptionClaims()
    {
        var mediator = new Mock<IMediator>();
        var expiresAt = DateTime.UtcNow.AddDays(14);
        mediator.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginUserResult(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Radu",
                "radu@example.com",
                SubscriptionPlan.Premium,
                SubscriptionStatus.Active,
                expiresAt));

        var controller = new AuthController(mediator.Object, BuildConfiguration());

        var result = await controller.Login(new LoginUserCommand("radu@example.com", "Secret123!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = GetPropertyValue<string>(ok.Value!, "Token");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("NutritionAdvisor", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == "subscription_plan" && c.Value == SubscriptionPlan.Premium.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == "subscription_status" && c.Value == SubscriptionStatus.Active.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == "subscription_expires_at");
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenMediatorThrows()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Invalid email or password."));

        var controller = new AuthController(mediator.Object, BuildConfiguration());

        var result = await controller.Login(new LoginUserCommand("radu@example.com", "bad"));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = GetPropertyValue<string>(badRequest.Value!, "Error");
        Assert.Contains("Invalid email or password", error);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "01234567890123456789012345678901",
                ["Jwt:Issuer"] = "NutritionAdvisor",
                ["Jwt:Audience"] = "NutritionAdvisorUsers"
            })
            .Build();
    }

    private static T GetPropertyValue<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return (T)property!.GetValue(value)!;
    }
}
