public interface IFileStorageService
{
    Task<string> UploadFileAsync(byte[] content, string fileName, CancellationToken cancellationToken);
}