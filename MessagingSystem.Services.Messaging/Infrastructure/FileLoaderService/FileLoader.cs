using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;

public class FileLoader : IFileLoader
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsSpaceSettings _settings;
    
    public FileLoader(IAmazonS3 s3Client, AwsSpaceSettings settings)
    {
        _s3Client = s3Client;
        _settings = settings;
    }
    
    public FileLoader(IOptions<AwsSpaceSettings> options)
    {
        _settings = options.Value;

        _s3Client = new AmazonS3Client(
            _settings.AccessKey,
            _settings.SecretKey,
            RegionEndpoint.GetBySystemName(_settings.Region)
        );
    }

    public async Task<string> GetUploadUrlAsync(string fileName, string? contentType = null)
    {
        var key = $"uploads/{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
        };

        if (!string.IsNullOrEmpty(contentType))
            request.ContentType = contentType;

        return await _s3Client.GetPreSignedURLAsync(request);
    }

    public async Task<string> GetDownloadUrlAsync(string fileName)
    {
        var key = $"uploads/{fileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };
        
        return await _s3Client.GetPreSignedURLAsync(request);
    }
    
    public async Task DeleteAsync(string fileName)
    {
        var key = $"uploads/{fileName}";
        await _s3Client.DeleteObjectAsync(_settings.BucketName, key);
    }
}