using System.IO;
using Microsoft.AspNetCore.Hosting;
using NutritionAdvisor.Infrastructure.Services;
using Xunit;

namespace NutritionAdvisor.Tests.Infrastructure;

public class LocalFileStorageServiceTests
{
    private class TestEnv : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Test";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider? WebRootFileProvider { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider? ContentRootFileProvider { get; set; }
    }

    [Fact]
    public async Task UploadFileAsync_WritesFileAndReturnsPath()
    {
        var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(temp);

        var env = new TestEnv
        {
            ContentRootPath = temp,
            WebRootPath = Path.Combine(temp, "wwwroot")
        };

        Directory.CreateDirectory(env.WebRootPath);

        var svc = new LocalFileStorageService(env);
        var content = new byte[] { 1, 2, 3 };
        var path = await svc.UploadFileAsync(content, "f.dat", CancellationToken.None);

        Assert.StartsWith("/UploadedFiles/", path);
        var physical = Path.Combine(env.WebRootPath, path.TrimStart('/'));
        Assert.True(File.Exists(physical));
        var data = await File.ReadAllBytesAsync(physical);
        Assert.Equal(content, data);

        // cleanup
        Directory.Delete(temp, true);
    }
}
