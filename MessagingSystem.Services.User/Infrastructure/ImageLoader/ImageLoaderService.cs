using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace MessagingSystem.Services.User.Infrastructure.ImageLoader;

public class ImageLoaderService : IImageLoaderService
{
    private readonly IAmazonS3 _s3Client;
    private readonly DigitalOceanSpacesSettings _settings;

    public ImageLoaderService(IOptions<DigitalOceanSpacesSettings> options)
    {
        _settings = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.Endpoint,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
    }
    
    public async Task<string> UploadOrReplaceAsync(Stream fileStream, string fileName)
    {
        var key = $"photos/{fileName}";
        
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = "image/jpeg",
            CannedACL = S3CannedACL.PublicRead
        });
        
        return $"{_settings.Endpoint}/{_settings.BucketName}/{key}";
    }
    
    public async Task DeleteAsync(string key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key
        };
        if (await FileExistsAsync(key))
        {
            await _s3Client.DeleteObjectAsync(request);
            Console.WriteLine("Deleted.");
        }
        else
        {
            Console.WriteLine("File does not exists.");
        }
    }
    private async Task<bool> FileExistsAsync(string key)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            });

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}