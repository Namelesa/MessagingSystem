using MessagingSystem.Services.Messaging.Application.FileLoader;
using MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.FileLoader
{
    public class FileLoaderOrchestratorTests
    {
        [Fact]
        public async Task GetLoadFileUrlAsync_ReturnsUrl()
        {
            // Arrange
            var fileName = "file.txt";
            var fileFormat = "text/plain";
            var expectedUrl = "https://example.com/upload/file.txt";

            var mockLoader = new Mock<IFileLoader>();
            mockLoader
                .Setup(x => x.GetUploadUrlAsync(fileName, fileFormat))
                .ReturnsAsync(expectedUrl);

            var orchestrator = new FileLoaderOrchestrator(mockLoader.Object);

            // Act
            var url = await orchestrator.GetLoadFileUrlAsync(fileName, fileFormat);

            // Assert
            Assert.Equal(expectedUrl, url);
        }

        [Fact]
        public async Task GetLoadFileUrlAsync_Throws_WhenUrlIsNull()
        {
            // Arrange
            var mockLoader = new Mock<IFileLoader>();
            mockLoader
                .Setup(x => x.GetUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var orchestrator = new FileLoaderOrchestrator(mockLoader.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => orchestrator.GetLoadFileUrlAsync("file.txt", "text/plain")
            );
        }

        [Fact]
        public async Task GetDownloadFileUrlAsync_ReturnsUrl()
        {
            // Arrange
            var fileName = "file.txt";
            var expectedUrl = "https://example.com/download/file.txt";

            var mockLoader = new Mock<IFileLoader>();
            mockLoader
                .Setup(x => x.GetDownloadUrlAsync(fileName))
                .ReturnsAsync(expectedUrl);

            var orchestrator = new FileLoaderOrchestrator(mockLoader.Object);

            // Act
            var url = await orchestrator.GetDownloadFileUrlAsync(fileName);

            // Assert
            Assert.Equal(expectedUrl, url);
        }

        [Fact]
        public async Task GetDownloadFileUrlAsync_Throws_WhenUrlIsNull()
        {
            // Arrange
            var mockLoader = new Mock<IFileLoader>();
            mockLoader
                .Setup(x => x.GetDownloadUrlAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var orchestrator = new FileLoaderOrchestrator(mockLoader.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => orchestrator.GetDownloadFileUrlAsync("file.txt")
            );
        }

        [Fact]
        public async Task DeleteFileAsync_CallsDeleteAsyncOnLoader()
        {
            // Arrange
            var fileName = "file.txt";
            var mockLoader = new Mock<IFileLoader>();
            mockLoader
                .Setup(x => x.DeleteAsync(fileName))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var orchestrator = new FileLoaderOrchestrator(mockLoader.Object);

            // Act
            await orchestrator.DeleteFileAsync(fileName);

            // Assert
            mockLoader.Verify(x => x.DeleteAsync(fileName), Times.Once);
        }
    }
}
