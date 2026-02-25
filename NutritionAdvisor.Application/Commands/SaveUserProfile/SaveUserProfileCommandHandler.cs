using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.Commands.SaveUserProfile;
using MediatR;
using System;
using NutritionAdvisor.Domain.Entities;
public class SaveUserProfileCommandHandler:IRequestHandler<SaveUserProfileCommand,Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    public async Task<Guid> Handle(SaveUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userProfile = new UserProfile
        {
            Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
            Name = request.Name,
            Gender = request.Gender,
            Age = request.Age,
            Height = request.Height,
            Weight = request.Weight,
            Allergies = request.Allergies

        };
        await _userProfileRepository.SaveAsync(userProfile);
        
        return userProfile.Id;

    }
    public SaveUserProfileCommandHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }
    
    
    
    
}