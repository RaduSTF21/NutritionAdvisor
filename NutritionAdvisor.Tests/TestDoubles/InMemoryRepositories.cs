using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Tests.TestDoubles;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _usersById = new();
    private readonly Dictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public User? LastSavedUser { get; private set; }

    public Task<User?> GetByIdAsync(Guid id)
    {
        _usersById.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _usersById[user.UserId] = user;
        _usersByEmail[user.Email] = user;
        LastSavedUser = user;
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        _usersByEmail.TryGetValue(email, out var user);
        return Task.FromResult(user);
    }

    public Task UpdateAsync(User user)
    {
        _usersById[user.UserId] = user;
        _usersByEmail[user.Email] = user;
        LastSavedUser = user;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryUserProfileRepository : IUserProfileRepository
{
    private readonly Dictionary<Guid, UserProfile> _profilesByUserId = new();

    public UserProfile? LastSavedProfile { get; private set; }

    public Task SaveAsync(UserProfile userProfile)
    {
        _profilesByUserId[userProfile.UserId] = userProfile;
        LastSavedProfile = userProfile;
        return Task.CompletedTask;
    }

    public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _profilesByUserId.TryGetValue(userId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        _profilesByUserId.TryGetValue(userId, out var profile);
        return Task.FromResult(profile);
    }
}
