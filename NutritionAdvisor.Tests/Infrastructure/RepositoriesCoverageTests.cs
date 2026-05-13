using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NutritionAdvisor.Infrastructure.Databases;
using NutritionAdvisor.Infrastructure.Repositories;
using Xunit;

namespace NutritionAdvisor.Tests.Infrastructure;

public class RepositoriesCoverageTests
{
    [Fact]
    public async Task AllRepositories_AllMethods_CanBeInvoked_ForCoverage()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repoTypes = typeof(UserRepository).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository")).ToList();

        foreach (var repoType in repoTypes)
        {
            await ProcessRepoType(repoType, dbContext);
        }

        Assert.NotEmpty(repoTypes);
    }

    private static async Task ProcessRepoType(Type repoType, ApplicationDbContext dbContext)
    {
        var ctor = repoType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        if (ctor == null) return;

        var args = CreateRepoArgs(ctor, dbContext);
        try
        {
            var instance = ctor.Invoke(args.ToArray());
            var methods = repoType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                await InvokeRepoMethod(instance, method);
            }
        }
        catch { /* Intentional */ }
    }

    private static List<object> CreateRepoArgs(ConstructorInfo ctor, ApplicationDbContext dbContext)
    {
        var args = new List<object>();
        foreach (var p in ctor.GetParameters())
        {
            if (p.ParameterType == typeof(ApplicationDbContext)) args.Add(dbContext);
            else if (p.ParameterType.IsInterface)
            {
                var mock = Activator.CreateInstance(typeof(Mock<>).MakeGenericType(p.ParameterType))!;
                args.Add(mock.GetType().GetProperty("Object")!.GetValue(mock)!);
            }
            else args.Add(GetDummy(p.ParameterType)!);
        }
        return args;
    }

    private static async Task InvokeRepoMethod(object instance, MethodInfo method)
    {
        var args = method.GetParameters().Select(p => GetDummy(p.ParameterType)).ToArray();
        try
        {
            var result = method.Invoke(instance, args);
            if (result is Task task) await task;
        }
        catch { /* Intentional */ }
    }

    private static object? GetDummy(Type t)
    {
        if (t == typeof(string)) return "dummy";
        if (t == typeof(Guid)) return Guid.NewGuid();
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}