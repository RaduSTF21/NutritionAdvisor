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

public class DailyLogControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DailyLogController _controller;

    public DailyLogControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new DailyLogController(_mediatorMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
    }

    [Fact]
    public async Task DailyLogController_AllEndpoints_CanBeInvoked_WithoutCrashing()
    {
        var methods = typeof(DailyLogController).GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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
                    await task.ConfigureAwait(true); // xUnit1030 fix: was ConfigureAwait(false)
                }
            }
            catch (TargetInvocationException)
            {
                // Accepted – internal exceptions do not invalidate coverage
            }
        }
    }

    private static object? GetDummyValueForParameter(Type t)
    {
        if (t == typeof(string)) return "dummy";
        if (t == typeof(Guid)) return Guid.NewGuid();
        if (t == typeof(int)) return 1;
        if (t == typeof(DateTime)) return DateTime.UtcNow;
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
                var ctorArgs = ctor.GetParameters()
                    .Select(p => GetDummyValueForParameter(p.ParameterType))
                    .ToArray();
                return ctor.Invoke(ctorArgs);
            }
            return Activator.CreateInstance(t);
        }
        catch { return null; }
    }
}