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
        // Verificăm dacă email-ul există deja
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null) throw new Exception("Acest email este deja folosit!");

        // Criptăm parola cu BCrypt
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Creăm user-ul
        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        // Salvăm user-ul
        await _userRepository.AddAsync(newUser, cancellationToken);

        // Creăm automat un UserProfile cu numele din registrare
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