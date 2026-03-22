using MediatR;

namespace NutritionAdvisor.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand(string Name, string Email, string Password) : IRequest<Guid>;