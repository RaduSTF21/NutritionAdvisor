using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;
using MediatR;
using System;
using UserProfileEntity = NutritionAdvisor.Domain.Entities.UserProfile;

public class SaveUserProfileCommandHandler:IRequestHandler<SaveUserProfileCommand,Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRepository _userRepository;
    public async Task<Guid> Handle(SaveUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = new UserProfileEntity
        {
            UserId = request.UserId,
            Name = request.Name,
            Gender = request.Gender ?? string.Empty,
            Objective = request.Objective, 
            Age = request.Age,
            Height = request.Height,
            Weight = request.Weight,
            Allergies = request.Allergies
        };

        await _userProfileRepository.SaveAsync(userProfile);
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user != null)
        {
            user.Name = request.Name;
            await _userRepository.UpdateAsync(user);
        }
        
        return userProfile.UserId;
    }
    public SaveUserProfileCommandHandler(IUserProfileRepository userProfileRepository, IUserRepository userRepository)
    {
        _userProfileRepository = userProfileRepository;
        _userRepository = userRepository;
    }
    
    
    
    
}