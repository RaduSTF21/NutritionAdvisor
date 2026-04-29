using MediatR;
using System.Text.Json.Serialization;

namespace NutritionAdvisor.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("Email")] string Email,
    [property: JsonPropertyName("Password")] string Password
) : IRequest<Guid>;