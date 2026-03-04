using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;
using MediatR;
using System;
using UserProfileEntity = NutritionAdvisor.Domain.Entities.UserProfile;

public class SaveUserProfileCommandHandler:IRequestHandler<SaveUserProfileCommand,Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    public async Task<Guid> Handle(SaveUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = new UserProfileEntity
        {
            UserId = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
            Name = request.Name,
            Gender = request.Gender ?? string.Empty,
            Age = request.Age,
            Height = request.Height,
            Weight = request.Weight,
            Allergies = request.Allergies

        };
        await _userProfileRepository.SaveAsync(userProfile);
        
        return userProfile.UserId;

    }
    public SaveUserProfileCommandHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }
    
    
    
    
}