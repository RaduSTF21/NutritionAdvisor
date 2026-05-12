using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;


namespace NutritionAdvisor.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserProfileRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(UserProfile userProfile)
    {
        // Check whether a profile already exists for this user.
        var existingProfile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userProfile.UserId);

        if (existingProfile != null)
        {
            // Keep the existing ID and update the remaining fields.
            existingProfile.Name = userProfile.Name;
            existingProfile.Gender = userProfile.Gender;
            existingProfile.Age = userProfile.Age;
            existingProfile.Height = userProfile.Height;
            existingProfile.Weight = userProfile.Weight;
            existingProfile.Allergies = userProfile.Allergies;
            existingProfile.Objective = userProfile.Objective;

            _dbContext.UserProfiles.Update(existingProfile);
        }
        else
        {
            // Insert a new profile.
            _dbContext.UserProfiles.Add(userProfile);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);
    }
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}