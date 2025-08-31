using MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;

namespace MessagingSystem.Services.Messaging.Application.FileLoader;

public class FileLoaderOrchestrator(
    IFileLoader fileLoaderService
    ) : IFileLoaderOrchestrator
{
    public async Task<string> GetLoadFileUrlAsync(string fileName, string fileFormat)
    {
        var url =  await fileLoaderService.GetUploadUrlAsync(fileName, fileFormat);
        return url ?? throw new InvalidOperationException("Failed to get load file URL");
    }
    public async Task<string> GetDownloadFileUrlAsync(string fileName)
    {
        var url = await fileLoaderService.GetDownloadUrlAsync(fileName);
        return url ?? throw new InvalidOperationException("Failed to get download file URL");
    }
    public Task DeleteFileAsync(string fileName)
    {
        return fileLoaderService.DeleteAsync(fileName);
    }
}