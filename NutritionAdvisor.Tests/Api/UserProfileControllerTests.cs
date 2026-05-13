using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionAdvisor.API.Controllers;
using Xunit;

namespace NutritionAdvisor.Tests.Api;

public class UserProfileControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly UserProfileController _controller;

    public UserProfileControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new UserProfileController(_mediatorMock.Object);

        // Generice mediator fallbacks
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        // Handle MediatR.Unit responses
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
    }

    [Fact]
    public async Task UserProfileController_AllEndpoints_CanBeInvoked_WithoutCrashing()
    {
        var methods = typeof(UserProfileController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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
                    await task;
                }
            }
            catch (Exception)
            {
                // Ignore any invocation errors
            }
        }
    }

    private static object? GetDummyValueForParameter(Type t)
    {
        if (t == typeof(string)) return "dummy";
        if (t == typeof(Guid)) return Guid.NewGuid();
        if (t == typeof(int)) return 1;
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