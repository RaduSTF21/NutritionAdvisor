using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;


namespace NutritionAdvisor.Infrastructure.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly ApplicationDbContext _dbContext;
    public async Task SaveAsync(UserProfile profile)
    {
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();
        
    }
    public UserProfileRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        
    }
    
}