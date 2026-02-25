using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Infrastructure.Databases;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        
    }
    public DbSet<UserProfile> UserProfiles { get; set; }
    
}