namespace MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;

public class AwsSpaceSettings
{
    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}