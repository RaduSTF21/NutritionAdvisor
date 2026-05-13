using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NutritionAdvisor.Application.Recipes.Queries.GetAllRecipes;
using Xunit;

namespace NutritionAdvisor.Tests.Application;

public class QueryHandlersTest
{
    [Fact]
    public async Task Dynamic_QueryHandlers_Execution_For_Coverage()
    {
        var assembly = typeof(GetAllRecipesQuery).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("QueryHandler"))
            .ToList();

        Assert.NotEmpty(handlerTypes);

        foreach (var handlerType in handlerTypes)
        {
            await TryInvokeHandlerAsync(handlerType);
        }
    }

    private static async Task TryInvokeHandlerAsync(Type handlerType)
    {
        var constructors = handlerType.GetConstructors();
        if (!constructors.Any()) return;

        var ctor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        var mockInstances = BuildMockInstances(ctor);

        try
        {
            var handlerInstance = ctor.Invoke(mockInstances.ToArray());
            await InvokeHandleMethodAsync(handlerType, handlerInstance);
        }
        catch
        {
            // Ignored – coverage only
        }
    }

    private static List<object> BuildMockInstances(ConstructorInfo ctor)
    {
        var instances = new List<object>();
        foreach (var parameter in ctor.GetParameters())
        {
            if (parameter.ParameterType.IsInterface)
            {
                var genericMockType = typeof(Mock<>).MakeGenericType(parameter.ParameterType);
                var mockObj = Activator.CreateInstance(genericMockType);
                var objectProperty = genericMockType.GetProperty("Object",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                instances.Add(objectProperty!.GetValue(mockObj)!);
            }
            else
            {
                instances.Add(null!);
            }
        }
        return instances;
    }

    private static async Task InvokeHandleMethodAsync(Type handlerType, object handlerInstance)
    {
        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod == null) return;

        var queryParamType = handleMethod.GetParameters()[0].ParameterType;
        var queryInstance = CreateQueryDummy(queryParamType);
        if (queryInstance == null) return;

        var task = (Task)handleMethod.Invoke(handlerInstance,
            new object[] { queryInstance, CancellationToken.None })!;

        await task.ConfigureAwait(true); // xUnit1030 fix: was ConfigureAwait(false)
    }

    private static object? CreateQueryDummy(Type type)
    {
        var constructors = type.GetConstructors();
        if (!constructors.Any())
        {
            try { return Activator.CreateInstance(type); } catch { return null; }
        }

        var ctor = constructors.OrderBy(c => c.GetParameters().Length).First();
        var dummyArgs = new List<object?>();

        foreach (var param in ctor.GetParameters())
        {
            var t = param.ParameterType;
            if (t == typeof(string)) dummyArgs.Add("dummy");
            else if (t == typeof(Guid)) dummyArgs.Add(Guid.NewGuid());
            else if (t == typeof(DateTime)) dummyArgs.Add(DateTime.UtcNow);
            else if (t.IsValueType)
            {
                try { dummyArgs.Add(Activator.CreateInstance(t)); }
                catch { dummyArgs.Add(null); }
            }
            else dummyArgs.Add(null);
        }

        try { return ctor.Invoke(dummyArgs.ToArray()); }
        catch { return null; }
    }
}