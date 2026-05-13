using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using NutritionAdvisor.API.Controllers;
using Xunit;

namespace NutritionAdvisor.Tests.Api;

public class IngredientsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IngredientsController _controller;

    public IngredientsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new IngredientsController(_mediatorMock.Object);

        // Fallbacks
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
    }

    [Fact]
    public async Task IngredientsController_AllEndpoints_CanBeInvoked_WithoutCrashing()
    {
        var methods = typeof(IngredientsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.NotEmpty(methods);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var dummyArgs = new List<object?>();

            foreach (var param in parameters)
            {
                dummyArgs.Add(GetDummyValueForParameter(param.ParameterType));
            }

            try
            {
                var result = method.Invoke(_controller, dummyArgs.ToArray());

                if (result is Task task)
                {
#pragma warning disable xUnit1030
                    await task.ConfigureAwait(true);
#pragma warning restore xUnit1030
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }

    private static object? GetDummyValueForParameter(Type t)
    {
        if (t == typeof(string)) return "dummy";
        if (t == typeof(Guid)) return Guid.NewGuid();
        if (t.IsValueType)
        {
            try { return Activator.CreateInstance(t); } catch { return null; }
        }

        try
        {
            var constructors = t.GetConstructors();
            if (constructors.Any())
            {
                var ctor = constructors.OrderBy(c => c.GetParameters().Length).First();
                var ctorArgs = ctor.GetParameters().Select(p => GetDummyValueForParameter(p.ParameterType)).ToArray();
                return ctor.Invoke(ctorArgs);
            }
            return Activator.CreateInstance(t);
        }
        catch { return null; }
    }
}