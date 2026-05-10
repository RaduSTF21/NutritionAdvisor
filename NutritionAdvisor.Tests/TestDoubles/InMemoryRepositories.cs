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

    public Task<User?> GetByProviderSubscriptionIdAsync(string providerSubscriptionId)
    {
        var user = _usersById.Values.FirstOrDefault(u => u.ProviderSubscriptionId == providerSubscriptionId);
        return Task.FromResult(user);
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

public sealed class InMemoryDailyLogRepository : NutritionAdvisor.Application.Interfaces.IDailyLogRepository
{
    private readonly Dictionary<Guid, NutritionAdvisor.Domain.Entities.DailyLog> _logsById = new();

    public Task<DailyLog?> GetByDateAsync(Guid userId, DateTime date, CancellationToken ct)
    {
        var log = _logsById.Values.FirstOrDefault(dl => dl.UserId == userId && dl.Date.Date == date.Date);
        return Task.FromResult(log);
    }

    public Task AddAsync(DailyLog dailyLog, CancellationToken ct)
    {
        _logsById[dailyLog.Id] = dailyLog;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DailyLog dailyLog, CancellationToken ct)
    {
        _logsById[dailyLog.Id] = dailyLog;
        return Task.CompletedTask;
    }

    public Task<DailyLog?> GetByMealIdAsync(Guid mealId, CancellationToken ct)
    {
        var log = _logsById.Values.FirstOrDefault(dl => dl.Meals.Any(m => m.Id == mealId));
        return Task.FromResult(log);
    }
}

public sealed class InMemoryRecipeRepository : NutritionAdvisor.Application.Interfaces.IRecipeRepository
{
    private readonly Dictionary<Guid, NutritionAdvisor.Domain.Entities.Recipe> _recipes = new();

    public Task AddAsync(NutritionAdvisor.Domain.Entities.Recipe recipe, CancellationToken ct)
    {
        _recipes[recipe.Id] = recipe;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        _recipes.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<NutritionAdvisor.Domain.Entities.Recipe>> FilterAsync(string? searchTerm, string? tag, NutritionAdvisor.Domain.Enums.Difficulty? level, CancellationToken ct)
    {
        return Task.FromResult(_recipes.Values.AsEnumerable());
    }

    public Task<IEnumerable<NutritionAdvisor.Domain.Entities.Recipe>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult(_recipes.Values.AsEnumerable());
    }

    public Task<NutritionAdvisor.Domain.Entities.Recipe?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _recipes.TryGetValue(id, out var r);
        return Task.FromResult(r);
    }

    public Task UpdateAsync(NutritionAdvisor.Domain.Entities.Recipe recipe, CancellationToken ct)
    {
        _recipes[recipe.Id] = recipe;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryMealPlanRepository : NutritionAdvisor.Application.Interfaces.IMealPlanRepository
{
    private readonly Dictionary<Guid, NutritionAdvisor.Domain.Entities.MealPlan> _plans = new();

    public Task AddAsync(NutritionAdvisor.Domain.Entities.MealPlan mealPlan, CancellationToken ct)
    {
        _plans[mealPlan.Id] = mealPlan;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
    {
        _plans.Remove(id);
        return Task.CompletedTask;
    }

    public Task<MealPlan?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _plans.TryGetValue(id, out var p);
        return Task.FromResult(p);
    }

    public Task<MealPlan?> GetByUserAndDateAsync(Guid userId, DateTime date, CancellationToken ct)
    {
        var plan = _plans.Values.FirstOrDefault(p => p.UserId == userId && p.Date.Date == date.Date);
        return Task.FromResult(plan);
    }

    public Task<IEnumerable<MealPlan>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var list = _plans.Values.Where(p => p.UserId == userId);
        return Task.FromResult(list);
    }
}

public sealed class InMemoryIngredientRepository : NutritionAdvisor.Application.Interfaces.IIngredientRepository
{
    private readonly Dictionary<Guid, NutritionAdvisor.Domain.Entities.Ingredient> _ingredients = new();

    public Task AddAsync(NutritionAdvisor.Domain.Entities.Ingredient ingredient, CancellationToken cancellationToken)
    {
        _ingredients[ingredient.Id] = ingredient;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<NutritionAdvisor.Domain.Entities.Ingredient>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_ingredients.Values.AsEnumerable());
    }

    public Task<NutritionAdvisor.Domain.Entities.Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _ingredients.TryGetValue(id, out var ing);
        return Task.FromResult(ing);
    }

    public Task<IEnumerable<NutritionAdvisor.Domain.Entities.Ingredient>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var res = _ingredients.Values.Where(i => i.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(res);
    }
}
