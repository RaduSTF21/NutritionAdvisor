using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Infrastructure.Databases;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the composite key for RecipeIngredient.
        modelBuilder.Entity<RecipeIngredient>()
            .HasKey(ri => new { ri.RecipeId, ri.IngredientId });

        // Configure the Recipe -> RecipeIngredients relationship.
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.Ingredients)
            .HasForeignKey(ri => ri.RecipeId);

        // Configure the Ingredient -> RecipeIngredients relationship.
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany() // Ingredients do not need a back-reference to recipes here.
            .HasForeignKey(ri => ri.IngredientId);
            
        // Store the Difficulty enum as a string for easier database readability.
        modelBuilder.Entity<Recipe>()
            .Property(r => r.Level)
            .HasConversion<string>();
    }
}