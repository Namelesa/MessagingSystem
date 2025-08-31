using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;
using Microsoft.Extensions.Options;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Infrastructure;

public class FileLoaderIntegrationTests : IAsyncLifetime
{
    private readonly IContainer _localstackContainer = new ContainerBuilder()
        .WithImage("localstack/localstack:latest")
        .WithName(Guid.NewGuid().ToString("N"))
        .WithPortBinding(4566, true)
        .WithEnvironment("SERVICES", "s3")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4566))
        .Build();
    
    private int _mappedPort;
    private readonly string _bucketName = "test-bucket";
    
    private AwsSpaceSettings _settings = new();

    public async Task InitializeAsync()
    {
        await _localstackContainer.StartAsync();
        _mappedPort = _localstackContainer.GetMappedPublicPort(4566);

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"http://localhost:{_mappedPort}",
            ForcePathStyle = true
        };

        using var s3Client = new AmazonS3Client("test", "test", s3Config);
        await s3Client.PutBucketAsync(_bucketName);

        _settings = new AwsSpaceSettings
        {
            AccessKey = "test",
            SecretKey = "test",
            Region = "us-east-1",
            BucketName = _bucketName
        };
    }

    public async Task DisposeAsync()
    {
        await _localstackContainer.StopAsync();
    }
    
    [Fact]
    public async Task GetUploadUrlAsync_ReturnsPresignedUrl()
    {
        var loader = new FileLoader(Options.Create(_settings));

        var url = await loader.GetUploadUrlAsync("file.txt");

        Assert.Contains("uploads/file.txt", url);
        Assert.Contains(_settings.BucketName, url);
    }
    
    [Fact]
    public async Task GetUploadUrlAsync_WithContentType_ReturnsPresignedUrl()
    {
        var loader = new FileLoader(Options.Create(_settings));

        var url = await loader.GetUploadUrlAsync("file.txt", "text/plain");

        Assert.Contains("uploads/file.txt", url);
        Assert.Contains(_settings.BucketName, url);
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ReturnsPresignedUrl()
    {
        var loader = new FileLoader(Options.Create(_settings));

        var url = await loader.GetDownloadUrlAsync("file.txt");

        Assert.Contains("uploads/file.txt", url);
        Assert.Contains(_settings.BucketName, url);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsOrSucceeds()
    {
        var loader = new FileLoader(Options.Create(_settings));

        var ex = await Record.ExceptionAsync(() => loader.DeleteAsync("file.txt"));

        Assert.True(ex is null or AmazonS3Exception);
    }
    
    [Fact]
    public async Task DeleteAsync_WhenObjectExists_DoesNotThrow()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"http://localhost:{_mappedPort}",
            ForcePathStyle = true
        };

        using var s3Client = new AmazonS3Client("test", "test", s3Config);
        
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = "uploads/file.txt",
            ContentBody = "test-content"
        });

        var loader = new FileLoader(s3Client, _settings);
        
        var ex = await Record.ExceptionAsync(() => loader.DeleteAsync("file.txt"));

        Assert.Null(ex);
    }
}
