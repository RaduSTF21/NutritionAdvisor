using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;
using NutritionAdvisor.Infrastructure.Repositories;
using Xunit;

namespace NutritionAdvisor.Tests.Infrastructure;

public class RepositoriesTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task IngredientRepository_AddAndGet_Works()
    {
        var ctx = CreateContext();
        var repo = new IngredientRepository(ctx);
        var ing = new Ingredient { Id = Guid.NewGuid(), Name = "Apple" };

        await repo.AddAsync(ing, CancellationToken.None);

        var all = await repo.GetAllAsync(CancellationToken.None);
        Assert.Contains(all, i => i.Name == "Apple");

        var byId = await repo.GetByIdAsync(ing.Id, CancellationToken.None);
        Assert.NotNull(byId);

        var search = await repo.SearchByNameAsync("app", CancellationToken.None);
        Assert.Contains(search, i => i.Name == "Apple");
    }

    [Fact]
    public async Task UserRepository_AddAndGet_Works()
    {
        var ctx = CreateContext();
        var repo = new UserRepository(ctx);
        var user = new User { UserId = Guid.NewGuid(), Email = "u@example.com" };

        await repo.AddAsync(user, CancellationToken.None);

        var byEmail = await repo.GetByEmailAsync("u@example.com", CancellationToken.None);
        Assert.NotNull(byEmail);

        var byId = await repo.GetByIdAsync(user.UserId);
        Assert.NotNull(byId);
    }
}
