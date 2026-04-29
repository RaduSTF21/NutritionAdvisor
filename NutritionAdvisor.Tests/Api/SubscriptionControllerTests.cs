using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.API.Controllers;

namespace NutritionAdvisor.Tests.Api;

public class SubscriptionControllerTests
{
    [Fact]
    public void GetCurrentSubscription_ReturnsCurrentClaimData()
    {
        var controller = new SubscriptionController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.Parse("11111111-1111-1111-1111-111111111111").ToString()),
                        new Claim("subscription_plan", "Premium"),
                        new Claim("subscription_status", "Active"),
                        new Claim("subscription_expires_at", DateTime.UtcNow.AddDays(10).ToString("O"))
                    }, "Bearer"))
                }
            }
        };

        var result = controller.GetCurrentSubscription();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value!;
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), GetPropertyValue<Guid>(payload, "UserId"));
        Assert.Equal("Premium", GetPropertyValue<string>(payload, "Plan"));
        Assert.Equal("Active", GetPropertyValue<string>(payload, "Status"));
        Assert.NotNull(GetPropertyValue<DateTime?>(payload, "ExpiresAt"));
    }

    [Fact]
    public void GetCurrentSubscription_ReturnsUnauthorized_WhenClaimsAreMissing()
    {
        var controller = new SubscriptionController();

        var result = controller.GetCurrentSubscription();

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static T GetPropertyValue<T>(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(value)!;
    }
}
