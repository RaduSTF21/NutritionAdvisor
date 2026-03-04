using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.UserProfiles.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery,UserProfile?>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public GetUserProfileQueryHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfile?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        return await _userProfileRepository.GetUserProfileAsync(request.UserId, cancellationToken);
    }    
    
}