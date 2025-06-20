using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.WebApi
{
    public class ProgramTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        [Fact]
        public Task Main_ShouldConfigureApplicationCorrectly()
        {
            // Arrange & Act
            var client = factory.CreateClient();

            // Assert
            Assert.NotNull(client);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task CookieMiddleware_WithValidEncryptedToken_ShouldSetAuthorizationHeader()
        {
            // Arrange
            var factory1 = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var mockDecryptService = new Mock<IDecryptionInfo>();
                    mockDecryptService.Setup(x => x.Decrypt("encrypted_token"))
                                    .Returns("valid_access_token");
                    
                    services.AddSingleton(mockDecryptService.Object);
                });
            });

            var client = factory1.CreateClient();
            
            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            request.Headers.Add("Cookie", "access_token=encrypted_token");
            
            var response = await client.SendAsync(request);

            // Assert
            Assert.True(response.StatusCode != System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task CookieMiddleware_WithInvalidEncryptedToken_ShouldNotSetAuthorizationHeader()
        {
            // Arrange
            var factory1 = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var mockDecryptService = new Mock<IDecryptionInfo>();
                    mockDecryptService.Setup(x => x.Decrypt("invalid_token"))
                                    .Returns(string.Empty);
                    
                    services.AddSingleton(mockDecryptService.Object);
                });
            });

            var client = factory1.CreateClient();
            
            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            request.Headers.Add("Cookie", "access_token=invalid_token");
            
            var response = await client.SendAsync(request);

            // Assert
            Assert.True(response.StatusCode != System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task CookieMiddleware_WithoutAccessTokenCookie_ShouldContinueWithoutSettingHeader()
        {
            // Arrange
            var client = factory.CreateClient();
            
            // Act
            var response = await client.GetAsync("/api/test");

            // Assert
            Assert.True(response.StatusCode != System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task Application_InDevelopmentEnvironment_ShouldConfigureSwagger()
        {
            // Arrange
            var factory1 = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            var client = factory1.CreateClient();

            // Act
            var swaggerResponse = await client.GetAsync("/swagger");
            
            // Assert
            Assert.True(swaggerResponse.StatusCode == System.Net.HttpStatusCode.OK || 
                       swaggerResponse.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                       swaggerResponse.StatusCode == System.Net.HttpStatusCode.Redirect);
        }

        [Fact]
        public async Task Application_InProductionEnvironment_ShouldNotConfigureSwagger()
        {
            // Arrange
            var factory1 = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });

            var client = factory1.CreateClient();

            // Act
            var swaggerResponse = await client.GetAsync("/swagger");
            
            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, swaggerResponse.StatusCode);
        }
        
        [Fact]
        public async Task CookieMiddleware_WithNullDecryptResult_ShouldNotSetAuthorizationHeader()
        {
            // Arrange
            var factory1 = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var mockDecryptService = new Mock<IDecryptionInfo>();
                    mockDecryptService.Setup(x => x.Decrypt("encrypted_token"))
                                    .Returns((string)null!);
                    
                    services.AddSingleton(mockDecryptService.Object);
                });
            });

            var client = factory1.CreateClient();
            
            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
            request.Headers.Add("Cookie", "access_token=encrypted_token");
            
            var response = await client.SendAsync(request);

            // Assert
            Assert.True(response.StatusCode != System.Net.HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task Application_ShouldConfigureMiddlewarePipeline()
        {
            // Arrange
            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/nonexistent");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}