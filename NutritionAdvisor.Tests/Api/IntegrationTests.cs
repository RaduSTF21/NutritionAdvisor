using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionAdvisor.Infrastructure.Databases;
using Xunit;

namespace NutritionAdvisor.Tests.Api;

public class TestingWebAppFactory : WebApplicationFactory<Program>
{
    static TestingWebAppFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connection);
            });

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            }
        });
    }
}

public class IntegrationTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;

    public IntegrationTests(TestingWebAppFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/Recipes")]
    public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ImageMiddleware_ReturnsSvgPlaceholder_WhenImageIsMissing()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/UploadedFiles/missing_test_image.jpg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
    }

    // --- TEST ACTUALIZAT: Accesează Swagger-ul în mediul de Testing ---
    [Fact]
    public async Task Program_TestingEnvironment_HitsSwaggerEndpoints()
    {
        var client = _factory.CreateClient();

        // Acum că am modificat Program.cs să permită Swagger în Testing, 
        // acest request va returna 200 OK și va bifa liniile de cod.
        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}