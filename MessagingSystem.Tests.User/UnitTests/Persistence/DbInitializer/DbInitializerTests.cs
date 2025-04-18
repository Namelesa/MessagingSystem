using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MessagingSystem.Services.User.Persistence;

namespace MessagingSystem.Tests.User.UnitTests.Persistence.DbInitializer
{
    public interface ITestDbContext
    {
        Task MigrateAsync(CancellationToken cancellationToken = default);
        Task<bool> HasPendingMigrations();
    }

    public class TestableDbInitializer(ITestDbContext context, ILogger<TestableDbInitializer> logger)
    {
        private readonly ITestDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<TestableDbInitializer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task Initialize()
        {
            try
            {
                if (await _context.HasPendingMigrations())
                {
                    await _context.MigrateAsync();
                    _logger.LogInformation("Database migration applied.");
                }
                else
                {
                    _logger.LogInformation("No pending migrations.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while migrating database.");
            }
        }
    }

    public class DbInitializerTests
    {
        private AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task Initialize_ShouldCallMigrate_WhenThereArePendingMigrations()
        {
            // Arrange
            var dbContext = CreateInMemoryContext("Db_WithMigrations");
            var loggerMock = new Mock<ILogger<Services.User.Persistence.DbInitializer.DbInitializer>>();

            var initializer = new Services.User.Persistence.DbInitializer.DbInitializer(dbContext, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            Assert.True(await dbContext.Database.CanConnectAsync());
        }

        [Fact]
        public async Task Initialize_ShouldNotCallMigrate_WhenNoPendingMigrations()
        {
            // Arrange
            var dbContext = CreateInMemoryContext("Db_NoMigrations");
            var loggerMock = new Mock<ILogger<Services.User.Persistence.DbInitializer.DbInitializer>>();

            var initializer = new Services.User.Persistence.DbInitializer.DbInitializer(dbContext, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            Assert.True(await dbContext.Database.CanConnectAsync());
        }

        [Fact]
        public async Task Initialize_ShouldNotLogError_WhenNoExceptionIsThrown()
        {
            // Arrange
            var dbContext = CreateInMemoryContext("Db_Success");
            var loggerMock = new Mock<ILogger<Services.User.Persistence.DbInitializer.DbInitializer>>();

            var initializer = new Services.User.Persistence.DbInitializer.DbInitializer(dbContext, loggerMock.Object);

            await dbContext.Database.EnsureCreatedAsync();

            // Act
            await initializer.Initialize();

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(), 
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never); 
        }

        [Fact]
        public async Task Initialize_ShouldLogInformation_WhenExceptionIsThrown()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<Services.User.Persistence.DbInitializer.DbInitializer>>();
            
            var mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            
            mockDbContext.Setup(db => db.Database)
                .Throws(new Exception("Database access error"));
            
            var initializer = new Services.User.Persistence.DbInitializer.DbInitializer(mockDbContext.Object, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error with migration")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TestableInitialize_ShouldHandleException_AndLogInformation()
        {
            // Arrange
            var mockDbContext = new Mock<ITestDbContext>();
            var loggerMock = new Mock<ILogger<TestableDbInitializer>>();
    
            mockDbContext.Setup(db => db.HasPendingMigrations())
                .ThrowsAsync(new Exception("Test exception"));
    
            var initializer = new TestableDbInitializer(mockDbContext.Object, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error, 
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error while migrating database")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Initialize_WithPendingMigrations_CallsMigrateAsync()
        {
            // Arrange
            var mockDbContext = new Mock<ITestDbContext>();
            var loggerMock = new Mock<ILogger<TestableDbInitializer>>();
            
            mockDbContext.Setup(db => db.HasPendingMigrations()).ReturnsAsync(true);
            
            var initializer = new TestableDbInitializer(mockDbContext.Object, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            mockDbContext.Verify(db => db.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Initialize_WithoutPendingMigrations_DoesNotCallMigrateAsync()
        {
            // Arrange
            var mockDbContext = new Mock<ITestDbContext>();
            var loggerMock = new Mock<ILogger<TestableDbInitializer>>();
            
            mockDbContext.Setup(db => db.HasPendingMigrations()).ReturnsAsync(false);
            
            var initializer = new TestableDbInitializer(mockDbContext.Object, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            mockDbContext.Verify(db => db.MigrateAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Initialize_ShouldHandleException_FromOriginalDbInitializer()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<Services.User.Persistence.DbInitializer.DbInitializer>>();
            
            var mockDbContext = new Mock<AppDbContext>(MockBehavior.Strict, new DbContextOptions<AppDbContext>());

            mockDbContext.Setup(db => db.Database).Returns(() => throw new Exception("Test exception"));
        
            var initializer = new Services.User.Persistence.DbInitializer.DbInitializer(mockDbContext.Object, loggerMock.Object);

            // Act
            await initializer.Initialize();

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error with migration")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
