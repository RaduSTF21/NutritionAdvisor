using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Domain.Enums;
using NutritionAdvisor.Infrastructure.Databases;
using NutritionAdvisor.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NutritionAdvisor.Tests.Infrastructure;

public sealed class RepositoriesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public RepositoriesTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
                _connection.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UserRepository_AddAndGet_Works()
    {
        var repo = new UserRepository(_context);
        var user = new User { UserId = Guid.NewGuid(), Email = "test@user.com", Name = "Test", PasswordHash = "hash" };

        await repo.AddAsync(user, CancellationToken.None);

        var fetched = await repo.GetByIdAsync(user.UserId);
        var byEmail = await repo.GetByEmailAsync("test@user.com", CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.NotNull(byEmail);
        Assert.Equal("Test", fetched.Name);
    }

    [Fact]
    public async Task RecipeRepository_CRUDAndFilter_Works()
    {
        var repo = new RecipeRepository(_context);
        var recipe = new Recipe { Id = Guid.NewGuid(), Title = "Test Recipe", Description = "Desc", Level = Difficulty.Easy };

        await repo.AddAsync(recipe, CancellationToken.None);

        var all = await repo.GetAllAsync(CancellationToken.None);
        Assert.NotEmpty(all);

        recipe.Title = "Updated";
        await repo.UpdateAsync(recipe, CancellationToken.None);

        var updated = await repo.GetByIdAsync(recipe.Id, CancellationToken.None);
        Assert.Equal("Updated", updated!.Title);

        await repo.DeleteAsync(recipe.Id, CancellationToken.None);
        var deleted = await repo.GetByIdAsync(recipe.Id, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task MealPlanRepository_AddGet_Works()
    {
        var userRepo = new UserRepository(_context);
        var planRepo = new MealPlanRepository(_context);

        var user = new User { UserId = Guid.NewGuid(), Email = "plan@test.com", Name = "Plan", PasswordHash = "hash" };
        await userRepo.AddAsync(user, CancellationToken.None);

        var date = DateTime.UtcNow.Date;
        var plan = new MealPlan { Id = Guid.NewGuid(), UserId = user.UserId, Date = date, TargetCalories = 2000 };
        await planRepo.AddAsync(plan, CancellationToken.None);

        var fetchedAll = await planRepo.GetByUserIdAsync(user.UserId, CancellationToken.None);
        Assert.Single(fetchedAll);

        await planRepo.DeleteAsync(plan.Id, CancellationToken.None);
        var deleted = await planRepo.GetByIdAsync(plan.Id, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task IngredientRepository_AddAndGet_Works()
    {
        var repo = new IngredientRepository(_context);
        var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "Tomato", Calories = 20 };

        await repo.AddAsync(ingredient, CancellationToken.None);

        var fetched = await repo.GetByIdAsync(ingredient.Id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("Tomato", fetched.Name);
    }
}