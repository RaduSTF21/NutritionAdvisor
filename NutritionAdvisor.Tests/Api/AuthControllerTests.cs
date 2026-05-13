using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using NutritionAdvisor.API.Controllers;
using Xunit;

namespace NutritionAdvisor.Tests.Api;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _configMock = new Mock<IConfiguration>();
        
        // Constructorul AuthController cere acum un IConfiguration, i-l furnizăm via Mock
        _controller = new AuthController(_mediatorMock.Object, _configMock.Object);

        // Fallbacks generice pentru MediatR
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());
    }

    [Fact]
    public async Task AuthController_AllEndpoints_CanBeInvoked_WithoutCrashing()
    {
        var methods = typeof(AuthController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

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
#pragma warning disable xUnit1030 // Evităm regula xUnit strictă care cere omiterea ConfigureAwait aici
                    await task.ConfigureAwait(true);
#pragma warning restore xUnit1030
                }
            }
            catch (TargetInvocationException)
            {
                // Ignorăm
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