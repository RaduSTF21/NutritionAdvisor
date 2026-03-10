using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using UserProfileEntity = NutritionAdvisor.Domain.Entities.UserProfile;

namespace NutritionAdvisor.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public RegisterUserCommandHandler(IUserRepository userRepository, IUserProfileRepository userProfileRepository)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check whether the email address is already registered.
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null) throw new Exception("This email address is already in use.");

        // Hash the password with BCrypt.
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create the user record.
        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        // Persist the user.
        await _userRepository.AddAsync(newUser, cancellationToken);

        // Automatically create a matching user profile.
        var userProfile = new UserProfileEntity
        {
            UserId = newUser.UserId,
            Name = request.Name,
            Gender = string.Empty,
            Age = 0,
            Height = 0,
            Weight = 0,
            Allergies = null,
            Objective = null
        };

        await _userProfileRepository.SaveAsync(userProfile);

        return newUser.UserId;
    }
}