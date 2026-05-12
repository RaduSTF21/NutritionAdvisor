using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Infrastructure.Databases;
using NutritionAdvisor.Infrastructure.Repositories;
using NutritionAdvisor.Infrastructure.Options;
using NutritionAdvisor.Infrastructure.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "fallback"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequirePremium", policy => policy.RequireClaim("subscription_plan", "Premium"));
    options.AddPolicy("RequireActiveSubscription", policy => policy.RequireAssertion(VerifyActiveSubscription));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile.SaveUserProfileCommand).Assembly));

builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IDailyLogRepository, DailyLogRepository>();
builder.Services.AddScoped<IMealPlanRepository, MealPlanRepository>();
builder.Services.AddScoped<IFoodPreferenceRepository, FoodPreferenceRepository>();
builder.Services.AddScoped<IAllergyRepository, AllergyRepository>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IFileStorageService, NutritionAdvisor.Infrastructure.Services.LocalFileStorageService>();

builder.Services.Configure<PythonAiOptions>(builder.Configuration.GetSection("PythonAI"));
builder.Services.AddHttpClient<IPythonAiService, PythonAiService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5210"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorOrigin", policy => policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// --- EXECUTĂM MIGRAȚIILE ---
ApplyDatabaseMigrations(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Nutrition Advisor API v1"));
}
else
{
    app.UseHttpsRedirection();
}

// --- MIDDLEWARE PENTRU IMAGINI ---
app.Use(HandleRecipePlaceholderImageAsync);

app.UseCors("AllowBlazorOrigin");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();


// ============================================================================
// --- METODE EXTRASE PENTRU REDUCEREA COMPLEXITĂȚII COGNITIVE (SonarCloud) ---
// ============================================================================

static bool VerifyActiveSubscription(AuthorizationHandlerContext context)
{
    if (!context.User.HasClaim(c => c.Type == "subscription_status" && c.Value == "Active"))
        return false;

    var expiresAtClaim = context.User.FindFirst("subscription_expires_at")?.Value;
    if (string.IsNullOrWhiteSpace(expiresAtClaim))
        return false;

    if (!DateTime.TryParse(expiresAtClaim, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt))
        return false;

    return expiresAt > DateTime.UtcNow;
}

static void ApplyDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    const int maxMigrationRetries = 10;

    for (var attempt = 1; attempt <= maxMigrationRetries; attempt++)
    {
        try
        {
            dbContext.Database.Migrate();
            break;
        }
        catch (Exception ex) when (attempt < maxMigrationRetries)
        {
            app.Logger.LogWarning(ex, "Database not ready yet. Retry {Attempt}/{MaxAttempts} in 3 seconds.", attempt, maxMigrationRetries);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

static async Task HandleRecipePlaceholderImageAsync(HttpContext context, Func<Task> next)
{
    if (context.Request.Path.StartsWithSegments("/UploadedFiles"))
    {
        // Variabila a fost creată pe baza structurii host-ului global
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var physicalPath = Path.Combine(webRoot, context.Request.Path.Value!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(physicalPath))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "image/svg+xml";
            context.Response.Headers.CacheControl = "no-store";

            const string svg = """<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800"><rect width="1200" height="800" fill="#f3f4f6"/><text x="600" y="400" text-anchor="middle" font-family="Arial" font-size="34" fill="#6b7280">No image</text></svg>""";
            await context.Response.WriteAsync(svg);
            return;
        }
    }
    await next();
}