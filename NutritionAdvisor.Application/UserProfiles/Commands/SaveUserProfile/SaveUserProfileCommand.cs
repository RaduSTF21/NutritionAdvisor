using MediatR;

namespace NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;

public class SaveUserProfileCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? Gender { get; init; }
    public required string Objective { get; init; }
    public int Age { get; init; }
    public double Weight { get; init; }
    public double Height { get; init; }
    public string? Allergies { get; init; }
}