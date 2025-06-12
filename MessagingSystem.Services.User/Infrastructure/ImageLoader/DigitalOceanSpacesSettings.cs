namespace MessagingSystem.Services.User.Infrastructure.ImageLoader;

public class DigitalOceanSpacesSettings
{
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
}