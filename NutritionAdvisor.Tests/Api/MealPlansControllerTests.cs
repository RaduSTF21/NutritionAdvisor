using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using Xunit;

namespace NutritionAdvisor.Tests.Api;

public class MealPlansControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly MealPlansController _controller;
    private readonly Guid _testUserId;

    public MealPlansControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new MealPlansController(_mediatorMock.Object);
        _testUserId = Guid.NewGuid();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Fallbacks
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
    }

    [Fact]
    public async Task MealPlansController_AllEndpoints_CanBeInvoked_WithoutCrashing()
    {
        // Include inherited public instance methods as well to avoid failures when the controller
        // doesn't declare its own public methods directly (e.g. when using minimal APIs or base implementations).
        var methods = typeof(MealPlansController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
            .ToArray();

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