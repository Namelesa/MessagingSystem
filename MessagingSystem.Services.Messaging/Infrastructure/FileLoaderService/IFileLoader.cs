namespace MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;

public interface IFileLoader
{ 
    Task<string> GetUploadUrlAsync(string fileName, string contentType);
    Task<string> GetDownloadUrlAsync(string fileName);
    Task DeleteAsync(string fileName);
}