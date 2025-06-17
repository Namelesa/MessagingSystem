using MessagingSystem.Services.Messaging.Persistence.Oto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Persistence.Oto
{
    public class OtoDbInitializerTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public async Task InitializeAsync() => await _pgContainer.StartAsync();
        public async Task DisposeAsync() => await _pgContainer.StopAsync();

        private OtoAppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<OtoAppDbContext>()
                .UseNpgsql(_pgContainer.GetConnectionString())
                .Options;

            return new OtoAppDbContext(options);
        }

        [Fact]
        public async Task Initialize_WhenDatabaseHasPendingMigrations_ShouldApplyMigrations()
        {
            // Arrange
            await using var context = CreateDbContext();
            await context.Database.EnsureDeletedAsync();
            var logger = new TestLogger<Services.Messaging.Persistence.Oto.OtoDbInitializer.OtoDbInitializer>();
            var initializer = new Services.Messaging.Persistence.Oto.OtoDbInitializer.OtoDbInitializer(context, logger);

            // Act
            await initializer.Initialize();

            // Assert
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            Assert.NotEmpty(appliedMigrations);
        }

        [Fact]
        public async Task Initialize_WhenNoPendingMigrations_ShouldNotChangeState()
        {
            // Arrange
            await using var context = CreateDbContext();
            var logger = new TestLogger<Services.Messaging.Persistence.Oto.OtoDbInitializer.OtoDbInitializer>();
            var initializer = new Services.Messaging.Persistence.Oto.OtoDbInitializer.OtoDbInitializer(context, logger);
            
            await initializer.Initialize();
            var migrationsBefore = (await context.Database.GetAppliedMigrationsAsync()).ToList();

            // Act
            await initializer.Initialize();
            var migrationsAfter = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            
            Assert.Equal(migrationsBefore, migrationsAfter);
        }

        [Fact]
        public async Task Initialize_WhenMigrationThrows_ShouldLogInformation()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<OtoAppDbContext>()
                .UseNpgsql("Host=localhost;Port=12345;Username=invalid;Password=invalid;Database=fail")
                .Options;
            await using var context = new OtoAppDbContext(options);
            var logger = new TestLogger<Services.Messaging.Persistence.Oto.OtoDbInitializer.OtoDbInitializer>();
            var initializer = new Services.Messaging.Persistence.Oto.OtoDbInitializer.OtoDbInitializer(context, logger);

            // Act
            await initializer.Initialize();

            // Assert
            Assert.Contains(logger.LoggedMessages, message => message.Contains("Error with migration"));
        }
        
        [Fact]
        public void TestLogger_BeginScope_ReturnsNullScopeInstance()
        {
            // Arrange
            var logger = new TestLogger<string>();
            
            // Act
            using var scope = logger.BeginScope("test scope");
            
            // Assert
            Assert.Same(NullScope.Instance, scope);
        }
        
        [Fact]
        public void TestLogger_IsEnabled_AlwaysReturnsTrue()
        {
            // Arrange
            var logger = new TestLogger<string>();
            
            // Act & Assert
            Assert.True(logger.IsEnabled(LogLevel.Debug));
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
        }
        
        [Fact]
        public void TestLogger_Log_WithNullFormatter_DoesNotAddToLoggedMessages()
        {
            // Arrange
            var logger = new TestLogger<string>();
            
            // Act
            logger.Log(LogLevel.Information, new EventId(1), "test state", null, null);
            
            // Assert
            Assert.Empty(logger.LoggedMessages);
        }
        
        [Fact]
        public void TestLogger_Log_WithFormatter_AddsMessageToLoggedMessages()
        {
            // Arrange
            var logger = new TestLogger<string>();
            var testMessage = "Test log message";
            
            // Act
            logger.Log(
                LogLevel.Information,
                new EventId(1),
                "state",
                null,
                (_, _) => testMessage
            );
            
            // Assert
            Assert.Contains(testMessage, logger.LoggedMessages);
        }
        
        [Fact]
        public void NullScope_Instance_IsSingleton()
        {
            // Act
            var instance1 = NullScope.Instance;
            var instance2 = NullScope.Instance;
            
            // Assert
            Assert.Same(instance1, instance2);
            Assert.IsType<NullScope>(instance1);
        }
        
        [Fact]
        public void NullScope_Constructor_CreatesInstance()
        {
            // Act
            var scope = new NullScope();
            
            // Assert
            Assert.NotNull(scope);
        }
        
        [Fact]
        public void NullScope_Dispose_DoesNotThrow()
        {
            // Arrange
            using (NullScope.Instance)
            {
                // Using the scope to ensure Dispose is called
            }

            // Act & Assert
            Assert.True(true);
        }
    }

    public class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string>? formatter)
        {
            if (formatter == null) return;
            var message = formatter(state, exception);
            LoggedMessages.Add(message);
        }
    }

    public class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        
        public void Dispose() { }
    }
}