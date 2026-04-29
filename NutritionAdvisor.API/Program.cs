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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequirePremium", policy =>
        policy.RequireClaim("subscription_plan", "Premium"));

    options.AddPolicy("RequireActiveSubscription", policy =>
        policy.RequireAssertion(context =>
        {
            if (!context.User.HasClaim(c => c.Type == "subscription_status" && c.Value == "Active"))
            {
                return false;
            }

            var expiresAtClaim = context.User.FindFirst("subscription_expires_at")?.Value;
            if (string.IsNullOrWhiteSpace(expiresAtClaim))
            {
                return false;
            }

            if (!DateTime.TryParse(
                expiresAtClaim,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
            {
                return false;
            }

            return expiresAt > DateTime.UtcNow;
        }));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile.SaveUserProfileCommand).Assembly));

builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IDailyLogRepository, DailyLogRepository>();
builder.Services.AddScoped<IFoodPreferenceRepository, FoodPreferenceRepository>();
builder.Services.AddScoped<IAllergyRepository, AllergyRepository>();
builder.Services.AddScoped<IFileStorageService, NutritionAdvisor.Infrastructure.Services.LocalFileStorageService>();
builder.Services.Configure<PythonAiOptions>(builder.Configuration.GetSection("PythonAI"));
builder.Services.AddHttpClient<IPythonAiService, PythonAiService>();

// --- CONFIGURARE CORS (Frontend in Docker ruleaza pe 5210) ---
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5210"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorOrigin",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

const string recipePlaceholderSvg = """
<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800" role="img" aria-label="Recipe image placeholder">
    <defs>
        <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stop-color="#f3f4f6"/>
            <stop offset="100%" stop-color="#e5e7eb"/>
        </linearGradient>
    </defs>
    <rect width="1200" height="800" fill="url(#bg)" rx="36"/>
    <circle cx="600" cy="310" r="108" fill="#d1d5db"/>
    <path d="M530 312c0-39 31-70 70-70s70 31 70 70-31 70-70 70-70-31-70-70zm36 0c0 19 15 34 34 34s34-15 34-34-15-34-34-34-34 15-34 34z" fill="#9ca3af"/>
    <path d="M380 510h440c29 0 52 23 52 52v28c0 29-23 52-52 52H380c-29 0-52-23-52-52v-28c0-29 23-52 52-52z" fill="#cbd5e1"/>
    <path d="M430 560h340" stroke="#94a3b8" stroke-width="18" stroke-linecap="round"/>
    <path d="M430 602h250" stroke="#94a3b8" stroke-width="18" stroke-linecap="round"/>
    <text x="600" y="690" text-anchor="middle" font-family="Arial, Helvetica, sans-serif" font-size="34" fill="#6b7280">No recipe image available</text>
</svg>
""";

// --- MIGRARE AUTOMATĂ (Apare o singură dată) ---
using (var scope = app.Services.CreateScope())
{
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
            app.Logger.LogWarning(ex,
                "Database not ready yet. Retry {Attempt}/{MaxAttempts} in 3 seconds.",
                attempt,
                maxMigrationRetries);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Nutrition Advisor API v1");
    });
}
else
{
    // Https redirection se face de obicei doar cand nu suntem in Development (sau in Docker pe HTTP)
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/UploadedFiles"))
    {
        var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var physicalPath = Path.Combine(webRoot, context.Request.Path.Value!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(physicalPath))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "image/svg+xml";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync(recipePlaceholderSvg);
            return;
        }
    }

    await next();
});

app.UseCors("AllowBlazorOrigin");
app.UseStaticFiles(); // O singura data
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();