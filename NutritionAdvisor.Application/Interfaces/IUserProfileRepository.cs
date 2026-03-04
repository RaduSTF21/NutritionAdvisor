using UserProfileEntity = NutritionAdvisor.Domain.Entities.UserProfile;

namespace NutritionAdvisor.Application.Interfaces;

public interface IUserProfileRepository
{
    Task SaveAsync(UserProfileEntity userProfile);
    Task<UserProfileEntity?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}