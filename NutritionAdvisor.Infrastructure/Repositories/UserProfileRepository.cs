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
    
    public async Task SaveAsync(UserProfile profile)
    {
        // Verificăm dacă există deja un profile pentru acest user
        var existingProfile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == profile.UserId);
        
        if (existingProfile != null)
        {
            // Update: păstrăm Id-ul existent și actualizăm restul câmpurilor
            existingProfile.Name = profile.Name;
            existingProfile.Gender = profile.Gender;
            existingProfile.Age = profile.Age;
            existingProfile.Height = profile.Height;
            existingProfile.Weight = profile.Weight;
            existingProfile.Allergies = profile.Allergies;
            existingProfile.Objective = profile.Objective;
            
            _dbContext.UserProfiles.Update(existingProfile);
        }
        else
        {
            // Insert: adăugăm un profil nou
            _dbContext.UserProfiles.Add(profile);
        }
        
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}