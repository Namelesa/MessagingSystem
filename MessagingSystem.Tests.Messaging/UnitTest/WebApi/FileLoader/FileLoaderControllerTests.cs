using MessagingSystem.Services.Messaging.Application.FileLoader;
using MessagingSystem.Services.Messaging.WebApi.FileLoader;
using MessagingSystem.Services.Messaging.WebApi.FileLoader.FileLoadContract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.FileLoader
{
    public class FileLoaderControllerTests
    {
        [Fact]
        public async Task GetLoadFileUrlsAsync_ReturnsBadRequest_WhenFilesAreNull()
        {
            // Arrange
            var mockOrchestrator = new Mock<IFileLoaderOrchestrator>();
            var controller = new FileLoaderController(mockOrchestrator.Object);

            var files = new FileLoaderMessage { File = null };

            // Act
            var result = await controller.GetLoadFileUrlsAsync(files);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Files are required", badRequestResult.Value);
        }

        [Fact]
        public async Task GetLoadFileUrlsAsync_ReturnsUrls()
        {
            // Arrange
            var mockOrchestrator = new Mock<IFileLoaderOrchestrator>();
            var controller = new FileLoaderController(mockOrchestrator.Object);
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.ContentType).Returns("text/plain");
            mockFile.Setup(f => f.Length).Returns(10);
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[10]));

            var files = new FileLoaderMessage
            {
                File = new[] { mockFile.Object }
            };

            mockOrchestrator
                .Setup(x => x.GetLoadFileUrlAsync("test.txt", "text/plain"))
                .ReturnsAsync("https://example.com/upload/test.txt");

            // Act
            var result = await controller.GetLoadFileUrlsAsync(files);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var urls = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);

            var first = Assert.Single(urls);
            Assert.Contains("test.txt", first.ToString());
        }
        
        [Fact]
        public async Task GetDownloadFileUrlsAsync_ReturnsUrls()
        {
            // Arrange
            var mockOrchestrator = new Mock<IFileLoaderOrchestrator>();
            var controller = new FileLoaderController(mockOrchestrator.Object);

            var fileNames = new[] { "file1.txt", "file2.txt" };

            mockOrchestrator
                .Setup(x => x.GetDownloadFileUrlAsync("file1.txt"))
                .ReturnsAsync("https://example.com/download/file1.txt");

            mockOrchestrator
                .Setup(x => x.GetDownloadFileUrlAsync("file2.txt"))
                .ReturnsAsync("https://example.com/download/file2.txt");

            // Act
            var result = await controller.GetDownloadFileUrlsAsync(fileNames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var urls = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, ((List<object>)urls).Count);
        }
        
        [Fact]
        public async Task DeleteFileAsync_CallsOrchestratorAndReturnsNoContent()
        {
            // Arrange
            var mockOrchestrator = new Mock<IFileLoaderOrchestrator>();
            var controller = new FileLoaderController(mockOrchestrator.Object);

            var fileNames = new[] { "file1.txt", "file2.txt" };

            // Act
            var result = await controller.DeleteFileAsync(fileNames);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            
            mockOrchestrator.Verify(x => x.DeleteFileAsync("file1.txt"), Times.Once);
            mockOrchestrator.Verify(x => x.DeleteFileAsync("file2.txt"), Times.Once);
        }
    }
}
