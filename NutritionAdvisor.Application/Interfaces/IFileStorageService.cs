using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(byte[] content, string fileName, CancellationToken cancellationToken);
}
