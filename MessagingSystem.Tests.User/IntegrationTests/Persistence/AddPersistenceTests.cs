using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Persistence;
using MessagingSystem.Services.User.Persistence.DbInitializer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessagingSystem.Tests.User.IntegrationTests.Persistence
{
    public class AddPersistenceLayerTests
    {
        private readonly ServiceCollection _serviceCollection;
        private readonly IConfiguration _configuration;

        public AddPersistenceLayerTests()
        {
            _serviceCollection = new ServiceCollection();
            
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Host=my_host;Database=my_db;Username=my_user;Password=my_pw" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void AddPersistenceLayer_Should_Register_Services()
        {
            // Act
            _serviceCollection.AddPersistenceLayer(_configuration);

            var serviceProvider = _serviceCollection.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<AppDbContext>());
            Assert.NotNull(serviceProvider.GetService<IUserRepository>());
            Assert.NotNull(serviceProvider.GetService<IDbInitializer>());
        }

        [Fact]
        public void AddPersistenceLayer_Should_Configure_DbContext_For_PostgreSQL()
        {
            // Act
            _serviceCollection.AddPersistenceLayer(_configuration);
            var serviceProvider = _serviceCollection.BuildServiceProvider();

            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            // Assert
            var databaseProvider = dbContext.Database.ProviderName;
            Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", databaseProvider);
        }
    }
}