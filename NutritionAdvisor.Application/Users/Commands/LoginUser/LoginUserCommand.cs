using MediatR;

namespace NutritionAdvisor.Application.Users.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<LoginUserResult>;

public record LoginUserResult(Guid UserId, string Name, string Email);
