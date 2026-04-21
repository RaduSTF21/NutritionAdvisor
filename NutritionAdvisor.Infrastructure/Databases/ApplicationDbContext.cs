using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public DbSet<DailyLog> DailyLogs { get; set; }
    public DbSet<Meal> Meals { get; set; }
    public DbSet<FoodPreference> FoodPreferences { get; set; }
    public DbSet<Allergy> Allergies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) =>
                left == right ||
                (left != null && right != null && left.SequenceEqual(right)),
            value => value == null
                ? 0
                : value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            value => value == null ? new List<string>() : value.ToList());

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

        // Configure the DailyLog -> ConsumedRecipes relationship.
        modelBuilder.Entity<DailyLog>()
            .HasMany(dl => dl.Meals)
            .WithOne(m => m.DailyLog)
            .HasForeignKey(m => m.LogId);

        // Configure the Meal -> Recipe relationship.
        modelBuilder.Entity<Meal>()
            .HasOne(m => m.Recipe)
            .WithMany(r => r.Meals)
            .HasForeignKey(m => m.RecipeId);

        modelBuilder.Entity<FoodPreference>()
            .Property(fp => fp.DietType)
            .HasConversion<string>();

        var preferredCuisinesProperty = modelBuilder.Entity<FoodPreference>()
            .Property(fp => fp.PreferredCuisines)
            .HasConversion(
                value => string.Join("||", value),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : value.Split("||", StringSplitOptions.RemoveEmptyEntries).ToList());
        preferredCuisinesProperty.Metadata.SetValueComparer(stringListComparer);

        var dislikedIngredientsProperty = modelBuilder.Entity<FoodPreference>()
            .Property(fp => fp.DislikedIngredients)
            .HasConversion(
                value => string.Join("||", value),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : value.Split("||", StringSplitOptions.RemoveEmptyEntries).ToList());
        dislikedIngredientsProperty.Metadata.SetValueComparer(stringListComparer);

        modelBuilder.Entity<Allergy>()
            .Property(a => a.SeverityLevel)
            .HasConversion<string>();

        modelBuilder.Entity<Allergy>()
            .Property(a => a.AllergenName)
            .HasMaxLength(100);

        modelBuilder.Entity<Allergy>()
            .HasIndex(a => new { a.UserId, a.AllergenName })
            .IsUnique();
    }
}