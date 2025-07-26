namespace MessagingSystem.Services.Messaging.Infrastructure.ImageLoader;

public interface IImageLoaderService
{
    Task<string> UploadOrReplaceAsync(Stream fileStream, string fileName);
    Task DeleteAsync(string fileName);
}