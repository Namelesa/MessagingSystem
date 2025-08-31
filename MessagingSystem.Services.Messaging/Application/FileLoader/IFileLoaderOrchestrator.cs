namespace MessagingSystem.Services.Messaging.Application.FileLoader;

public interface IFileLoaderOrchestrator
{
    Task<string> GetLoadFileUrlAsync(string fileName, string fileFormat);
    Task<string> GetDownloadFileUrlAsync(string fileName);
    Task DeleteFileAsync(string fileName);
}