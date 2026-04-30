using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.Tests.Domain;

public class UserTests
{
    [Fact]
    public void NewUser_HasFreeSubscriptionDefaults()
    {
        var user = new User();

        Assert.Equal(SubscriptionPlan.Free, user.SubscriptionPlan);
        Assert.Equal(SubscriptionStatus.Inactive, user.SubscriptionStatus);
        Assert.False(user.AutoRenew);
        Assert.Null(user.ProviderCustomerId);
        Assert.Null(user.ProviderSubscriptionId);
    }
}
