using NutritionAdvisor.Application.Users.Commands.RegisterUser;
using NutritionAdvisor.Domain.Enums;
using NutritionAdvisor.Tests.TestDoubles;

namespace NutritionAdvisor.Tests.Application;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesUserAndProfile_WithFreeSubscriptionDefaults()
    {
        var userRepository = new InMemoryUserRepository();
        var profileRepository = new InMemoryUserProfileRepository();
        var handler = new RegisterUserCommandHandler(userRepository, profileRepository);

        var userId = await handler.Handle(new RegisterUserCommand("Radu", "radu@example.com", "Secret123!"), CancellationToken.None);

        var savedUser = await userRepository.GetByIdAsync(userId);
        var savedProfile = await profileRepository.GetByUserIdAsync(userId);

        Assert.NotNull(savedUser);
        Assert.NotNull(savedProfile);
        Assert.Equal("Radu", savedUser!.Name);
        Assert.Equal("radu@example.com", savedUser.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify("Secret123!", savedUser.PasswordHash));
        Assert.Equal(SubscriptionPlan.Free, savedUser.SubscriptionPlan);
        Assert.Equal(SubscriptionStatus.Inactive, savedUser.SubscriptionStatus);
        Assert.Equal(userId, savedProfile!.UserId);
        Assert.Equal("Radu", savedProfile.Name);
        Assert.Equal(string.Empty, savedProfile.Gender);
    }

    [Fact]
    public async Task Handle_Throws_WhenEmailAlreadyExists()
    {
        var userRepository = new InMemoryUserRepository();
        var profileRepository = new InMemoryUserProfileRepository();
        var handler = new RegisterUserCommandHandler(userRepository, profileRepository);

        await handler.Handle(new RegisterUserCommand("Radu", "radu@example.com", "Secret123!"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(new RegisterUserCommand("Another", "radu@example.com", "Secret456!"), CancellationToken.None));

        Assert.Contains("already in use", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
