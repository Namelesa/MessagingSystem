using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace MessagingSystem.Tests.Notification.UnitTests.Infrastructure
{
    public class TemplateReaderTests
    {
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly TemplateReader _templateReader;

        public TemplateReaderTests()
        {
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _templateReader = new TemplateReader(_mockWebHostEnvironment.Object);
        }

        [Fact]
        public async Task ReadTemplateAsync_ReturnsFileContent_WhenFileExists()
        {
            // Arrange
            const string templatePath = "templates/testTemplate.html";
            var contentRootPath = Directory.GetCurrentDirectory();
            var fullPath = Path.Combine(contentRootPath, templatePath);
            
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, "Test Content");

            _mockWebHostEnvironment.Setup(w => w.ContentRootPath).Returns(contentRootPath);

            try
            {
                // Act
                var result = await _templateReader.ReadTemplateAsync(templatePath);

                // Assert
                Assert.Equal("Test Content", result);
            }
            finally
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
        }

        [Fact]
        public async Task ReadTemplateAsync_ReturnsNull_WhenFileDoesNotExist()
        {
            // Arrange
            const string templatePath = "templates/nonExistentTemplate.html";
            var contentRootPath = Directory.GetCurrentDirectory();

            _mockWebHostEnvironment.Setup(w => w.ContentRootPath).Returns(contentRootPath);

            // Act
            var result = await _templateReader.ReadTemplateAsync(templatePath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadTemplateAsync_HandlesExceptionsGracefully()
        {
            // Arrange
            const string templatePath = "templates/errorTemplate.html";
            Directory.GetCurrentDirectory();
            
            _mockWebHostEnvironment.Setup(w => w.ContentRootPath).Returns(() 
                => throw new DirectoryNotFoundException("Mocked exception for testing."));

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _templateReader.ReadTemplateAsync(templatePath));
        }
    }
}