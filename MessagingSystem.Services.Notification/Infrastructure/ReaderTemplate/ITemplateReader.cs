namespace MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;

public interface ITemplateReader
{
    Task<string?> ReadTemplateAsync(string templatePath);
}