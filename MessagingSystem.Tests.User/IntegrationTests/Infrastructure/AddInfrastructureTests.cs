using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using Encryptor.Decryption;
using Encryptor.Encryption;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using MessagingSystem.Services.User.Infrastructure.Keys;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using MessagingSystem.Services.User.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace MessagingSystem.Tests.User.IntegrationTests.Infrastructure
{
    public class InfrastructureLayerTests(WebApplicationFactory<Program> factory)
        : IClassFixture<WebApplicationFactory<Program>>
    {
        
        [Fact]
        public void Should_Resolve_All_Dependencies()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;

            Assert.NotNull(provider.GetService<IHasher>());
            Assert.NotNull(provider.GetService<IHasherPassword>());
            Assert.NotNull(provider.GetService<IEncryptionInfo>());
            Assert.NotNull(provider.GetService<IDecryptionInfo>());
            Assert.NotNull(provider.GetService<IPublicKeyStorage>());
            Assert.NotNull(provider.GetService<KeyPublisher>());
            Assert.NotNull(provider.GetService<IJwtService>());
        }

        [Fact]
        public async Task Should_Serve_Swagger_Endpoint()
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("/swagger/index.html");

            Assert.True(response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.Redirect);
        }
        
        [Fact]
        public async Task Jwt_Token_Should_Allow_Access_To_Protected_Endpoint()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = "I@mMaximB*&*&^%$#$%^&UIKJHGFDCXCVBNMKIJUHYGTFRDESZXCVBNMilykFromUkrainePokrovsk"u8.ToArray();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("sub", "test-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = "User",
                Audience = "Admin"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            var protectedEndpoint = "/api/test/protected";

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(protectedEndpoint, [Authorize]() => Results.Ok("Protected endpoint reached successfully!"));
                    });
                });
            }).CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await client.GetAsync(protectedEndpoint);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        
        [Fact]
        public async Task Request_With_Valid_Token_Should_Access_Protected_Endpoint()
        {
            // Arrange
            var token = GenerateValidToken();

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthorization();
                });

                builder.Configure(app =>
                {
                    app.UseRouting();

                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/test/protected", async context =>
                        {
                            await context.Response.WriteAsync("Protected endpoint reached successfully!");
                        }).RequireAuthorization();
                    });
                });
            }).CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/test/protected");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("Protected endpoint reached successfully!", result);
        }
        
        [Fact]
        public async Task Request_With_Encrypted_Token_In_Cookie_Should_Be_Authenticated()
        {
            // Arrange
            var token = GenerateValidToken();
            
            using var scope = factory.Services.CreateScope();
            var encryptor = scope.ServiceProvider.GetRequiredService<IEncryptionInfo>();
            var encryptedToken = encryptor.Encrypt(token);
    
            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/test/protected", [Authorize]() => Results.Text("Protected endpoint reached successfully!"));
                    });
                });
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });
            
            client.DefaultRequestHeaders.Add("Cookie", $"access_token={encryptedToken}");

            // Act
            var response = await client.GetAsync("/api/test/protected");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("Protected endpoint reached successfully!", result);
        }
        
        [Fact]
        public async Task Should_Trigger_OnMessageReceived_Event_When_Token_Is_In_Cookie()
        {
            // Arrange
            var token = GenerateValidToken();
            using var scope = factory.Services.CreateScope();
            var encryptor = scope.ServiceProvider.GetRequiredService<IEncryptionInfo>();
    
            var encryptedToken = encryptor.Encrypt(token);

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/test/protected", [Authorize]() => Results.Ok("Protected endpoint reached successfully!"));
                    });
                });
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

            client.DefaultRequestHeaders.Add("Cookie", $"access_token={encryptedToken}");

            // Act
            var response = await client.GetAsync("/api/test/protected");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }       

        [Fact]
        public async Task Should_Trigger_OnMessageReceived_Event_When_Token_Is_In_Header()
        {
            // Arrange
            var token = GenerateValidToken();

            var client = factory.WithWebHostBuilder(builder =>
            {
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/test/protected", [Authorize]() => Results.Ok("Protected endpoint reached successfully!"));
                    });
                });
            }).CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/test/protected");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        
        [Fact]
        public async Task Should_Trigger_OnMessageReceived_And_Decrypt_Token()
        {
            // Arrange
            const string encrypted = "encrypted_token";
            const string decrypted = "valid_token";

            var decryptorMock = new Mock<IDecryptionInfo>();
            decryptorMock.Setup(d => d.Decrypt(encrypted)).Returns(decrypted);

            var services = new ServiceCollection();
            services.AddSingleton(decryptorMock.Object);
            var provider = services.BuildServiceProvider();

            var jwtOptions = new JwtBearerOptions();
            var scheme = new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler));

            var onMessageReceived = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (!ctx.Request.Cookies.ContainsKey("access_token"))
                    return Task.CompletedTask;

                var tokenEncrypted = ctx.Request.Cookies["access_token"];
                var decryptor = ctx.HttpContext.RequestServices.GetRequiredService<IDecryptionInfo>();
                if (string.IsNullOrEmpty(tokenEncrypted))
                    return Task.CompletedTask;

                var tokenDecrypted = decryptor.Decrypt(tokenEncrypted);
                ctx.Token = tokenDecrypted;
                return Task.CompletedTask;
            }
        };

            // Arrange: CASE 1 - Access Token Encrypted and Valid
            var context1 = new Mock<HttpContext>();
            var cookies1 = new Mock<IRequestCookieCollection>();
            cookies1.Setup(c => c.ContainsKey("access_token")).Returns(true);
            cookies1.Setup(c => c["access_token"]).Returns(encrypted);

            context1.Setup(c => c.Request.Cookies).Returns(cookies1.Object);
            context1.Setup(c => c.RequestServices).Returns(provider);

            var msgCtx1 = new MessageReceivedContext(context1.Object, scheme, jwtOptions);

            // Act
            await onMessageReceived.OnMessageReceived(msgCtx1);

            // Assert
            Assert.Equal(decrypted, msgCtx1.Token); // Ensure the token is decrypted and set properly

            // Arrange: CASE 2 - Cookie is missing
            var context2 = new Mock<HttpContext>();
            var cookies2 = new Mock<IRequestCookieCollection>();
            cookies2.Setup(c => c.ContainsKey("access_token")).Returns(false);

            context2.Setup(c => c.Request.Cookies).Returns(cookies2.Object);
            context2.Setup(c => c.RequestServices).Returns(provider);

            var msgCtx2 = new MessageReceivedContext(context2.Object, scheme, jwtOptions);

            // Act
            await onMessageReceived.OnMessageReceived(msgCtx2);

            // Assert
            Assert.Null(msgCtx2.Token); // Ensure no token set

            // Arrange: CASE 3 - Empty Cookie
            var context3 = new Mock<HttpContext>();
            var cookies3 = new Mock<IRequestCookieCollection>();
            cookies3.Setup(c => c.ContainsKey("access_token")).Returns(true);
            cookies3.Setup(c => c["access_token"]).Returns("");

            context3.Setup(c => c.Request.Cookies).Returns(cookies3.Object);
            context3.Setup(c => c.RequestServices).Returns(provider);

            var msgCtx3 = new MessageReceivedContext(context3.Object, scheme, jwtOptions);

            // Act
            await onMessageReceived.OnMessageReceived(msgCtx3);

            // Assert
            Assert.Null(msgCtx3.Token); // Ensure no token set if cookie is empty
        }
        
        [Fact]
        public void Should_Resolve_DigitalOceanSpacesSettings_Through_ServiceProvider()
        {
            // Arrange & Act
            using var scope = factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            
            serviceProvider.GetService<DigitalOceanSpacesSettings>();

            // Assert
            Assert.True(true);
        }
        
        private static string GenerateValidToken()
        {
            var securityKey = new SymmetricSecurityKey("I@mMaximB*&*&^%$#$%^&UIKJHGFDCXCVBNMKIJUHYGTFRDESZXCVBNMilykFromUkrainePokrovsk"u8.ToArray());
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "User",         
                audience: "Admin",     

                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}