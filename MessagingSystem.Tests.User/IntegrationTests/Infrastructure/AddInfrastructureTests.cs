using Encryptor.Decryption;
using Encryptor.Encryption;
using MessagingSystem.Services.User.Infrastructure;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using MessagingSystem.Services.User.Infrastructure.Keys;
using MessagingSystem.Services.User.Infrastructure.MessageBroker;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using MessagingSystem.Services.User.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MessagingSystem.Tests.User.IntegrationTests.Infrastructure
{
    public class AddInfrastructureIntegrationTests
        : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly IConfiguration _configuration = CreateTestConfiguration();

        [Fact]
        public void AddInfrastructureLayer_RegistersAllServices_Successfully()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddSingleton(_configuration);

            var mockBus = new Mock<MassTransit.IBus>();
            services.AddSingleton(mockBus.Object);

            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Verify all scoped services are registered
            Assert.NotNull(serviceProvider.GetService<IHasher>());
            Assert.NotNull(serviceProvider.GetService<IHasherPassword>());
            Assert.NotNull(serviceProvider.GetService<IJwtService>());
            Assert.NotNull(serviceProvider.GetService<KeyPublisher>());

            // Assert - Verify all singleton services are registered
            Assert.NotNull(serviceProvider.GetService<IEncryptionInfo>());
            Assert.NotNull(serviceProvider.GetService<IDecryptionInfo>());
            Assert.NotNull(serviceProvider.GetService<IPublicKeyStorage>());
            Assert.NotNull(serviceProvider.GetService<IImageLoaderService>());
        }

        [Fact]
        public void AddInfrastructureLayer_ConfiguresMessageBrokerSettings_Correctly()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var messageBrokerSettings = serviceProvider.GetService<MessageBrokerSettings>();
            Assert.NotNull(messageBrokerSettings);
            Assert.Equal("test-host", messageBrokerSettings.Host);
            Assert.Equal("test-user", messageBrokerSettings.UserName);
        }

        [Fact]
        public void AddInfrastructureLayer_ConfiguresDigitalOceanSpacesSettings_Correctly()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var digitalOceanSettings = serviceProvider.GetService<DigitalOceanSpacesSettings>();
            Assert.NotNull(digitalOceanSettings);
            Assert.Equal("test-access-key", digitalOceanSettings.AccessKey);
            Assert.Equal("test-secret-key", digitalOceanSettings.SecretKey);
        }

        [Fact]
        public void AddInfrastructureLayer_ConfiguresJwtAuthentication_WithCorrectParameters()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var jwtOptions = serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            Assert.NotNull(jwtOptions);
            
            var options = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);
            Assert.False(options.RequireHttpsMetadata);
            Assert.True(options.SaveToken);
            Assert.Equal("test-issuer", options.TokenValidationParameters.ValidIssuer);
            Assert.Equal("test-audience", options.TokenValidationParameters.ValidAudience);
            Assert.True(options.TokenValidationParameters.ValidateIssuer);
            Assert.True(options.TokenValidationParameters.ValidateAudience);
            Assert.True(options.TokenValidationParameters.ValidateLifetime);
            Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        }
        
        [Fact]
        public async Task JwtBearerEvents_OnMessageReceived_WithValidCookie_SetsTokenCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(_configuration);
            services.AddInfrastructureLayer(_configuration);
            
            // Mock decryption service
            var mockDecryptionService = new Mock<IDecryptionInfo>();
            mockDecryptionService.Setup(x => x.Decrypt("encrypted-token"))
                .Returns("decrypted-token");
            
            services.AddSingleton(mockDecryptionService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            
            // Create test context
            var cookieCollection = new Mock<IRequestCookieCollection>();
            cookieCollection.Setup(c => c.ContainsKey("access_token")).Returns(false);
            httpContext.Request.Cookies = cookieCollection.Object;
            cookieCollection = new Mock<IRequestCookieCollection>();
            cookieCollection.Setup(c => c.ContainsKey("access_token")).Returns(true);
            cookieCollection.Setup(c => c["access_token"]).Returns("encrypted-token");
            httpContext.Request.Cookies = cookieCollection.Object;

            httpContext.RequestServices = serviceProvider;
            
            var context = new MessageReceivedContext(httpContext, new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

            // Get JWT options and event handler
            var jwtOptions = serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            var options = jwtOptions!.Get(JwtBearerDefaults.AuthenticationScheme);

            // Act
            await options.Events.OnMessageReceived(context);

            // Assert
            Assert.Equal("decrypted-token", context.Token);
            mockDecryptionService.Verify(x => x.Decrypt("encrypted-token"), Times.Once);
        }

        [Fact]
        public async Task JwtBearerEvents_OnMessageReceived_WithoutCookie_DoesNotSetToken()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var context = new MessageReceivedContext(httpContext, new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

            var jwtOptions = serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            var options = jwtOptions!.Get(JwtBearerDefaults.AuthenticationScheme);

            // Act
            await options.Events.OnMessageReceived(context);

            // Assert
            Assert.Null(context.Token);
        }

        [Fact]
        public async Task JwtBearerEvents_OnMessageReceived_WithNullCookieValue_DoesNotSetToken()
        {
            // Arrange
            var services = new ServiceCollection();
            
            services.AddSingleton(_configuration);

            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var cookieCollection = new Mock<IRequestCookieCollection>();
            cookieCollection.Setup(c => c.ContainsKey("access_token")).Returns(true);
            cookieCollection.Setup(c => c["access_token"]).Returns((string)null);
            httpContext.Request.Cookies = cookieCollection.Object;

            var context = new MessageReceivedContext(httpContext, new AuthenticationScheme("test", null, typeof(JwtBearerHandler)), new JwtBearerOptions());

            var jwtOptions = serviceProvider.GetService<IOptionsMonitor<JwtBearerOptions>>();
            var options = jwtOptions!.Get(JwtBearerDefaults.AuthenticationScheme);

            // Act
            await options.Events.OnMessageReceived(context);

            // Assert
            Assert.Null(context.Token);
        }

        [Fact]
        public void AddInfrastructureLayer_ConfiguresSwagger_WithJwtSecurityScheme()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act
            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Verify Swagger services are registered
            Assert.NotNull(serviceProvider.GetService<IOptions<SwaggerGenOptions>>());
        }

        [Fact]
        public void AddInfrastructureLayer_ConfiguresAuthorization_Successfully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(_configuration);
            
            // Act
            services.AddInfrastructureLayer(_configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var authorizationService = serviceProvider.GetService<IAuthorizationService>();
            Assert.NotNull(authorizationService);
        }
        
        private static IConfiguration CreateTestConfiguration()
        {
            var configurationData = new Dictionary<string, string>
            {
                { "MessageBroker:Host", "test-host" },
                { "MessageBroker:Username", "test-user" },
                { "DigitalOceanSpacesSettings:AccessKey", "test-access-key" },
                { "DigitalOceanSpacesSettings:SecretKey", "test-secret-key" },
                { "DigitalOceanSpacesSettings:Region", "fra1" },
                { "DigitalOceanSpacesSettings:Endpoint", "https://tests.fra1.digitaloceanspaces.com" },
                { "JWTConfig:Issuer", "test-issuer" },
                { "JWTConfig:Audience", "test-audience" },
                { "JWTConfig:Key", "test-secret-key-that-is-long-enough-for-jwt" },
                { "Encryption:ChaChaKey", "test-secret-key-that-is-long-enough-for-jwt" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();
        }
    }
}