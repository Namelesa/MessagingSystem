using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Infrastructure
{
    public class ImageLoaderServiceTests
    {
        private readonly Mock<IOptions<DigitalOceanSpacesSettings>> _mockOptions;
        private readonly Mock<ILogger<ImageLoaderService>> _mockLogger;
        private readonly Mock<IAmazonS3> _mockS3Client;
        private readonly DigitalOceanSpacesSettings _settings;

        public ImageLoaderServiceTests()
        {
            _mockOptions = new Mock<IOptions<DigitalOceanSpacesSettings>>();
            _mockLogger = new Mock<ILogger<ImageLoaderService>>();
            _mockS3Client = new Mock<IAmazonS3>();

            _settings = new DigitalOceanSpacesSettings
            {
                Endpoint = "https://test-endpoint.com",
                BucketName = "test-bucket",
                AccessKey = "test-access-key",
                SecretKey = "test-secret-key",
                Region = "test-region"
            };

            _mockOptions.Setup(x => x.Value).Returns(_settings);
        }

        private ImageLoaderService CreateServiceWithMockedS3()
        {
            var service = new ImageLoaderService(_mockOptions.Object, _mockLogger.Object);
            
            var s3ClientField = typeof(ImageLoaderService)
                .GetField("_s3Client", BindingFlags.NonPublic | BindingFlags.Instance);
            s3ClientField?.SetValue(service, _mockS3Client.Object);
            
            return service;
        }

        [Fact]
        public async Task UploadOrReplaceAsync_ShouldCallPutObjectAsync_AndReturnCorrectUrl()
        {
            // Arrange
            var service = CreateServiceWithMockedS3();
            var fileName = "test-image.jpg";
            var expectedKey = $"photos/{fileName}";
            var expectedUrl = $"{_settings.Endpoint}/{_settings.BucketName}/{expectedKey}";
            
            using var stream = new MemoryStream();
            
            _mockS3Client
                .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
                .Returns(Task.FromResult(new PutObjectResponse()));

            // Act
            var result = await service.UploadOrReplaceAsync(stream, fileName);

            // Assert
            Assert.Equal(expectedUrl, result);
            
            _mockS3Client.Verify(x => x.PutObjectAsync(
                It.Is<PutObjectRequest>(req => 
                    req.BucketName == _settings.BucketName &&
                    req.Key == expectedKey &&
                    req.InputStream == stream &&
                    req.ContentType == "image/jpeg" &&
                    req.CannedACL == S3CannedACL.PublicRead), 
                default), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenFileExists_ShouldDeleteFile_AndLogSuccess()
        {
            // Arrange
            var service = CreateServiceWithMockedS3();
            var key = "photos/test-image.jpg";
            
            _mockS3Client
                .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
                .Returns(Task.FromResult(new GetObjectMetadataResponse()));
            
            _mockS3Client
                .Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default))
                .Returns(Task.FromResult(new DeleteObjectResponse()));

            // Act
            await service.DeleteAsync(key);

            // Assert
            _mockS3Client.Verify(x => x.DeleteObjectAsync(
                It.Is<DeleteObjectRequest>(req => 
                    req.BucketName == _settings.BucketName &&
                    req.Key == key), 
                default), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Deleted.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenFileDoesNotExist_ShouldNotDeleteFile_AndLogNotFound()
        {
            // Arrange
            var service = CreateServiceWithMockedS3();
            var key = "photos/non-existent-image.jpg";
            
            _mockS3Client
                .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
                .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

            // Act
            await service.DeleteAsync(key);

            // Assert
            _mockS3Client.Verify(x => x.DeleteObjectAsync(
                It.IsAny<DeleteObjectRequest>(), 
                default), Times.Never);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File does not exist, nothing to delete.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task FileExistsAsync_WhenFileExists_ShouldReturnTrue()
        {
            // Arrange
            var service = CreateServiceWithMockedS3();
            var key = "photos/existing-image.jpg";
            
            _mockS3Client
                .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
                .Returns(Task.FromResult(new GetObjectMetadataResponse()));

            // Act
            var result = await InvokeFileExistsAsync(service, key);

            // Assert
            Assert.True(result);
            
            _mockS3Client.Verify(x => x.GetObjectMetadataAsync(
                It.Is<GetObjectMetadataRequest>(req => 
                    req.BucketName == _settings.BucketName &&
                    req.Key == key), 
                default), Times.Once);
        }

        [Fact]
        public async Task FileExistsAsync_WhenFileDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var service = CreateServiceWithMockedS3();
            var key = "photos/non-existent-image.jpg";
            
            _mockS3Client
                .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
                .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

            // Act
            var result = await InvokeFileExistsAsync(service, key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FileExistsAsync_WhenOtherS3Exception_ShouldThrow()
        {
            // Arrange
            var service = CreateServiceWithMockedS3();
            var key = "photos/test-image.jpg";
            var expectedException = new AmazonS3Exception("Access Denied") { StatusCode = HttpStatusCode.Forbidden };
            
            _mockS3Client
                .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), default))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AmazonS3Exception>(() => InvokeFileExistsAsync(service, key));
            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithCorrectSettings()
        {
            // Arrange & Act
            var service = new ImageLoaderService(_mockOptions.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(service);
            _mockOptions.Verify(x => x.Value, Times.AtLeastOnce);
        }

        [Fact]
        public void DigitalOceanSpacesSettings_AccessKey_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedAccessKey = "test-access-key-123";
            var settings = new DigitalOceanSpacesSettings
            {
                AccessKey = expectedAccessKey
            };

            // Act
            var actualAccessKey = settings.AccessKey;

            // Assert
            Assert.Equal(expectedAccessKey, actualAccessKey);
        }

        [Fact]
        public void DigitalOceanSpacesSettings_SecretKey_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedSecretKey = "test-secret-key-456";
            var settings = new DigitalOceanSpacesSettings
            {
                SecretKey = expectedSecretKey
            };

            // Act
            var actualSecretKey = settings.SecretKey;

            // Assert
            Assert.Equal(expectedSecretKey, actualSecretKey);
        }

        [Fact]
        public void DigitalOceanSpacesSettings_BucketName_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedBucketName = "test-bucket-789";
            var settings = new DigitalOceanSpacesSettings
            {
                BucketName = expectedBucketName
            };

            // Act
            var actualBucketName = settings.BucketName;

            // Assert
            Assert.Equal(expectedBucketName, actualBucketName);
        }

        [Fact]
        public void DigitalOceanSpacesSettings_Region_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedRegion = "us-east-1";
            var settings = new DigitalOceanSpacesSettings
            {
                Region = expectedRegion
            };

            // Act
            var actualRegion = settings.Region;

            // Assert
            Assert.Equal(expectedRegion, actualRegion);
        }

        [Fact]
        public void DigitalOceanSpacesSettings_Endpoint_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedEndpoint = "https://nyc3.digitaloceanspaces.com";
            var settings = new DigitalOceanSpacesSettings
            {
                Endpoint = expectedEndpoint
            };

            // Act
            var actualEndpoint = settings.Endpoint;

            // Assert
            Assert.Equal(expectedEndpoint, actualEndpoint);
        }

        [Fact]
        public void DigitalOceanSpacesSettings_DefaultValues_ShouldBeEmptyStrings()
        {
            // Arrange & Act
            var settings = new DigitalOceanSpacesSettings();

            // Assert
            Assert.Equal(string.Empty, settings.AccessKey);
            Assert.Equal(string.Empty, settings.SecretKey);
            Assert.Equal(string.Empty, settings.BucketName);
            Assert.Equal(string.Empty, settings.Region);
            Assert.Equal(string.Empty, settings.Endpoint);
        }

        private async Task<bool> InvokeFileExistsAsync(ImageLoaderService service, string key)
        {
            var method = typeof(ImageLoaderService)
                .GetMethod("FileExistsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<bool>)method.Invoke(service, new object[] { key });
            return await task;
        }
    }
}