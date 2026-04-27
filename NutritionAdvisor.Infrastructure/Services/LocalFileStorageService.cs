using NutritionAdvisor.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace NutritionAdvisor.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _storagePath = Path.Combine(webRoot, "UploadedFiles");

        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> UploadFileAsync(byte[] content, string fileName, CancellationToken cancellationToken)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

        var physicalPath = Path.Combine(_storagePath, uniqueFileName);

        await File.WriteAllBytesAsync(physicalPath, content, cancellationToken);

        return $"/UploadedFiles/{uniqueFileName}";
    }
}