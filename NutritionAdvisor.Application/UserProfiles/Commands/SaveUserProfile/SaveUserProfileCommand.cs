using MediatR;

namespace NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;

public class SaveUserProfileCommand : IRequest<Guid>
{
    // Am eliminat proprietatea "Id" care nu era folosită corect
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public string? Gender { get; init; }
    public required string Objective { get; init; } // Required pentru a forța AI-ul să aibă date
    public int Age { get; init; }
    public double Weight { get; init; }
    public double Height { get; init; }
    public string? Allergies { get; init; }
}