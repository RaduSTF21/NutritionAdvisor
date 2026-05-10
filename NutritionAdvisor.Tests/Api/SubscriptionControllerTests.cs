using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Tests.Api;

public class SubscriptionControllerTests
{
    [Fact]
    public async Task GetCurrentSubscription_ReturnsCurrentClaimData()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                UserId = userId,
                SubscriptionPlan = SubscriptionPlan.Premium,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionEndAt = DateTime.UtcNow.AddDays(10),
                AutoRenew = true
            });

        var controller = new SubscriptionController(userRepository.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim("subscription_plan", "Premium"),
                        new Claim("subscription_status", "Active"),
                        new Claim("subscription_expires_at", DateTime.UtcNow.AddDays(10).ToString("O"))
                    }, "Bearer"))
                }
            }
        };

        var result = await controller.GetCurrentSubscription();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        Assert.Equal(userId, GetPropertyValue<Guid>(payload, "UserId"));
        Assert.Equal("Premium", GetPropertyValue<string>(payload, "Plan"));
        Assert.Equal("Active", GetPropertyValue<string>(payload, "Status"));
        Assert.NotNull(GetPropertyValue<DateTime?>(payload, "ExpiresAt"));
        Assert.True(GetPropertyValue<bool>(payload, "AutoRenew"));
    }

    [Fact]
    public async Task GetCurrentSubscription_ReturnsUnauthorized_WhenClaimsAreMissing()
    {
        var controller = new SubscriptionController(Mock.Of<IUserRepository>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var result = await controller.GetCurrentSubscription();

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static T GetPropertyValue<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(value)!;
    }
}
