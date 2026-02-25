using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IUserProfileRepository
{
    Task SaveAsync(UserProfile userProfile);
}