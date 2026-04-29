using MediatR;
using NutritionAdvisor.Domain.Enums;
using System.Text.Json.Serialization;

namespace NutritionAdvisor.Application.Users.Commands.LoginUser;

public record LoginUserCommand(
    [property: JsonPropertyName("Email")] string Email,
    [property: JsonPropertyName("Password")] string Password
) : IRequest<LoginUserResult>;

public record LoginUserResult(
    Guid UserId,
    string Name,
    string Email,
    SubscriptionPlan SubscriptionPlan,
    SubscriptionStatus SubscriptionStatus,
    DateTime? SubscriptionEndAt);
