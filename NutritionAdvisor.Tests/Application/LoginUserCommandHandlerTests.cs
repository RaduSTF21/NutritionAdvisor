using NutritionAdvisor.Application.Users.Commands.LoginUser;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;
using NutritionAdvisor.Tests.TestDoubles;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Tests.Application;

public class LoginUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSubscriptionData_WhenCredentialsAreValid()
    {
        var userRepository = new InMemoryUserRepository();
        var handler = new LoginUserCommandHandler(userRepository);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Name = "Radu",
            Email = "radu@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123!"),
            SubscriptionPlan = SubscriptionPlan.Premium,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionEndAt = DateTime.UtcNow.AddDays(30)
        };

        await userRepository.AddAsync(user, CancellationToken.None);

        var result = await handler.Handle(new LoginUserCommand("radu@example.com", "Secret123!"), CancellationToken.None);

        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(SubscriptionPlan.Premium, result.SubscriptionPlan);
        Assert.Equal(SubscriptionStatus.Active, result.SubscriptionStatus);
        Assert.Equal(user.SubscriptionEndAt, result.SubscriptionEndAt);
    }

    [Fact]
    public async Task Handle_Throws_WhenPasswordIsInvalid()
    {
        var userRepository = new InMemoryUserRepository();
        var handler = new LoginUserCommandHandler(userRepository);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Name = "Radu",
            Email = "radu@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret123!")
        };

        await userRepository.AddAsync(user, CancellationToken.None);

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(new LoginUserCommand("radu@example.com", "wrong"), CancellationToken.None));
    }
}
