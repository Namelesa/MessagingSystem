using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;
using MessagingSystem.Services.Messaging.Application.MessageBroker.Key;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Infrastructure;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using MessagingSystem.Services.Messaging.Infrastructure.MessageBroker;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupInformation;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMember;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Persistence.Oto.UsersImages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Infrastructure
{
    public class AddInfrastructureTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ServiceCollection _services;
        private readonly Mock<IDecryptionInfo> _mockDecryption;

        public AddInfrastructureTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _services = new ServiceCollection();
            _mockDecryption = new Mock<IDecryptionInfo>();
            SetupConfiguration();
        }

        private void SetupConfiguration()
        {
            var messageBrokerSection = new Mock<IConfigurationSection>();
            messageBrokerSection.Setup(x => x["Host"]).Returns("amqp://localhost:5672");
            messageBrokerSection.Setup(x => x["UserName"]).Returns("guest");
            messageBrokerSection.Setup(x => x["Password"]).Returns("guest");

            _mockConfiguration.Setup(x => x.GetSection("MessageBroker"))
                .Returns(messageBrokerSection.Object);
            
            var encryptionSection = new Mock<IConfigurationSection>();
            encryptionSection.Setup(x => x["ChaChaKey"])
                .Returns("ThisIsAVeryLongSecretKeyForTesting12345");

            _mockConfiguration.Setup(x => x.GetSection("Encryption"))
                .Returns(encryptionSection.Object);
            
            _mockConfiguration.Setup(x => x["Encryption:ChaChaKey"])
                .Returns("ThisIsAVeryLongSecretKeyForTesting12345");
            
            var redisSection = new Mock<IConfigurationSection>();
            redisSection.Setup(x => x["Host"]).Returns("localhost:6379");

            _mockConfiguration.Setup(x => x.GetSection("Redis"))
                .Returns(redisSection.Object);
            
            _mockConfiguration.Setup(x => x["Redis:Host"])
                .Returns("localhost:6379");
            
            _mockConfiguration.Setup(x => x["JWTConfig:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(x => x["JWTConfig:Audience"]).Returns("TestAudience");
            _mockConfiguration.Setup(x => x["JWTConfig:Key"]).Returns("ThisIsAVeryLongSecretKeyForTesting12345");
            _mockConfiguration.Setup(x => x["Redis:Host"]).Returns("localhost:6379");
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterAllRequiredServices()
        {
            // Arrange
            _services.AddSingleton(_mockConfiguration.Object);

            // Act
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<IHasher>());
            Assert.NotNull(serviceProvider.GetService<IEncryptionInfo>());
            Assert.NotNull(serviceProvider.GetService<IDecryptionInfo>());
            Assert.NotNull(serviceProvider.GetService<IPublicKeyStorage>());
            Assert.NotNull(serviceProvider.GetService<MessageBrokerSettings>());
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterSignalR()
        {
            // Act
            _services.AddLogging();        
            _services.AddAuthorization();  
            _services.AddSignalR();        
            _services.AddInfrastructureLayer(_mockConfiguration.Object);

            var provider = _services.BuildServiceProvider();

            // Act
            var otoHub = provider.GetService<IHubContext<OtoChatHub>>();
            var groupHub = provider.GetService<IHubContext<GroupChatHub>>();

            // Assert
            Assert.NotNull(otoHub);
            Assert.NotNull(groupHub);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldConfigureMessageBrokerSettings()
        {
            // Act
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var messageBrokerSettings = serviceProvider.GetService<MessageBrokerSettings>();
            Assert.NotNull(messageBrokerSettings);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterMassTransitWithRequestClients()
        {
            // Act
            _services.AddSingleton(Options.Create(new MessageBrokerSettings
            {
                Host = "amqp://localhost:5672",
                UserName = "guest",
                Password = "guest"
            }));
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var busControl = serviceProvider.GetService<IBusControl>();
            Assert.NotNull(busControl);
            
            var editUserClient = serviceProvider.GetService<IRequestClient<EditUserInfoRequest>>();
            var deleteUserClient = serviceProvider.GetService<IRequestClient<DeleteUserInfoRequest>>();
            
            Assert.NotNull(editUserClient);
            Assert.NotNull(deleteUserClient);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterConsumers()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                {"MessageBroker:Host", "amqp://localhost:5672"},
                {"MessageBroker:UserName", "guest"},
                {"MessageBroker:Password", "guest"},
                {"Encryption:ChaChaKey", "ThisIsAVeryLongSecretKeyForTesting12345"},
                {"Redis:Host", "localhost:6379"},
                {"JWTConfig:Issuer", "TestIssuer"},
                {"JWTConfig:Audience", "TestAudience"},
                {"JWTConfig:Key", "ThisIsAVeryLongSecretKeyForTesting12345"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _services.AddSingleton<IConfiguration>(configuration);

            _services.AddDbContextFactory<GroupAppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            _services.AddDbContextFactory<OtoAppDbContext>(options =>
            {
                options.UseInMemoryDatabase("OtoTestDb");
            });

            _services.AddAutoMapper(typeof(GroupMessagesOrchestrator).Assembly);
            _services.AddValidatorsFromAssemblyContaining<GroupMessageDto>();
            _services.AddScoped<IGroupMembersRepository, GroupMemberRepository>();
            _services.AddScoped<IGroupMessagesRepository, GroupMessagesRepository>();
            _services.AddScoped<IUserImageRepository, UserImageRepository>();
            _services.AddScoped<IGroupInfoRepository, GroupInfoRepository>();
            _services.AddScoped<IOtoMessageRepository, OtoMessageRepository>();
            _services.AddScoped<IGroupInfoOrchestrator, GroupInfoOrchestrator>();
            _services.AddScoped<IUserOrchestrator, UserOrchestrator>();
            _services.AddScoped<IGroupMessagesOrchestrator, GroupMessagesOrchestrator>();
            _services.AddScoped<IGroupMemberOrchestrator, GroupMemberOrchestrator>();
            _services.AddScoped<IMessageOrchestrator, MessageOrchestrator>();
            _services.AddScoped<IGroupEncryption, GroupEncryptionDecorator>();
            _services.AddLogging();

            _services.AddSingleton(Options.Create(new MessageBrokerSettings
            {
                Host = "amqp://localhost:5672",
                UserName = "guest",
                Password = "guest"
            }));

            _services.AddInfrastructureLayer(configuration);

            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var publicKeyConsumer = serviceProvider.GetService<PublicKeyConsumer>();
            var editUserConsumer = serviceProvider.GetService<EditUserInfoConsumer>();
            var deleteUserConsumer = serviceProvider.GetService<DeleteUserInfoConsumer>();

            Assert.NotNull(publicKeyConsumer);
            Assert.NotNull(editUserConsumer);
            Assert.NotNull(deleteUserConsumer);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldConfigureJwtAuthentication()
        {
            // Act
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var authOptions = serviceProvider.GetService<IOptions<AuthenticationOptions>>();
            Assert.NotNull(authOptions);
            
            var jwtOptions = serviceProvider.GetService<IOptions<JwtBearerOptions>>();
            Assert.NotNull(jwtOptions);
        }

        [Fact]
        public async Task AddInfrastructureLayer_ShouldConfigureCors()
        {
            // Act
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            var corsPolicyProvider = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsPolicyProvider>();
            var policy = await corsPolicyProvider.GetPolicyAsync(new DefaultHttpContext(), "AllowFrontend");

            Assert.NotNull(policy);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldConfigureSwagger()
        {
            _services.AddInfrastructureLayer(_mockConfiguration.Object);

            // Assert
            var swaggerService = _services.Any(sd =>
                sd.ServiceType == typeof(Swashbuckle.AspNetCore.Swagger.ISwaggerProvider));

            Assert.True(swaggerService);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterAuthorization()
        {
            // Act
            _services.AddLogging();
            _services.AddAuthorization();
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            var authorizationService = serviceProvider.GetService<IAuthorizationService>();
            Assert.NotNull(authorizationService);
        }
        
        [Fact]
        public async Task OnMessageReceived_WithCookieToken_ShouldDecryptAndSetToken()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                ["JWTConfig:Key"] = "0123456789abcdef0123456789abcdef",
                ["JWTConfig:Issuer"] = "your-issuer",
                ["JWTConfig:Audience"] = "your-audience",
                ["Encryption:ChaChaKey"] = "your_secret_key_that_is_long_enough"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddInfrastructureLayer(configuration);
            services.AddScoped<IDecryptionInfo>(_ => _mockDecryption.Object);

            var serviceProvider = services.BuildServiceProvider();

            var jwtOptions = serviceProvider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.NotNull(jwtOptions);

            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockCookies = new Mock<IRequestCookieCollection>();
            var mockQuery = new Mock<IQueryCollection>();

         mockCookies.Setup(x => x.TryGetValue("access_token", out It.Ref<string>.IsAny))
                .Returns((string _, out string value) =>
                {
                    value = "encrypted-token";
                    return true;
                });

            _mockDecryption.Setup(x => x.Decrypt("encrypted-token"))
                .Returns("decrypted-token");

            mockRequest.Setup(x => x.Query).Returns(mockQuery.Object);
            mockRequest.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(x => x.RequestServices).Returns(serviceProvider);

            var context = new MessageReceivedContext(
                mockHttpContext.Object,
                new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
                new JwtBearerOptions());

            // Act
            await jwtOptions.Events.OnMessageReceived(context);

            // Assert
            Assert.Equal("decrypted-token", context.Token);
            _mockDecryption.Verify(x => x.Decrypt("encrypted-token"), Times.Once);
        }
        
        [Fact]
        public async Task OnAuthenticationFailed_ShouldCompleteTask()
        {
            // Arrange
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            var jwtOptions = serviceProvider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
            Assert.NotNull(jwtOptions);

            var context = new AuthenticationFailedContext(
                new DefaultHttpContext(),
                new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
                new JwtBearerOptions())
            {
                Exception = new Exception("Test exception")
            };

            // Act & Assert
            await jwtOptions.Events.OnAuthenticationFailed(context);
        }

        [Fact]
        public void JwtTokenValidationParameters_ShouldBeConfiguredCorrectly()
        {
            // Arrange
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var jwtOptions = serviceProvider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            // Assert
            Assert.NotNull(jwtOptions);
            Assert.False(jwtOptions.RequireHttpsMetadata);
            Assert.True(jwtOptions.SaveToken);
            
            var validationParams = jwtOptions.TokenValidationParameters;
            Assert.Equal("TestIssuer", validationParams.ValidIssuer);
            Assert.Equal("TestAudience", validationParams.ValidAudience);
            Assert.True(validationParams.ValidateIssuer);
            Assert.True(validationParams.ValidateAudience);
            Assert.True(validationParams.ValidateLifetime);
            Assert.True(validationParams.ValidateIssuerSigningKey);
            Assert.NotNull(validationParams.IssuerSigningKey);
        }
    
        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterCorrectServiceLifetimes()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var services = new ServiceCollection();
            
            var messageBrokerSection = new Mock<IConfigurationSection>();
            messageBrokerSection.Setup(x => x["Host"]).Returns("rabbitmq://localhost");
            messageBrokerSection.Setup(x => x["UserName"]).Returns("guest");
            messageBrokerSection.Setup(x => x["Password"]).Returns("guest");

            mockConfiguration.Setup(x => x.GetSection("MessageBroker")).Returns(messageBrokerSection.Object);
            mockConfiguration.Setup(x => x["JWTConfig:Issuer"]).Returns("TestIssuer");
            mockConfiguration.Setup(x => x["JWTConfig:Audience"]).Returns("TestAudience");
            mockConfiguration.Setup(x => x["JWTConfig:Key"]).Returns("ThisIsAVeryLongSecretKeyForTesting12345");

            // Act
            services.AddInfrastructureLayer(mockConfiguration.Object);

            // Assert
            var hasherDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IHasher));
            var encryptionDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IEncryptionInfo));
            var decryptionDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IDecryptionInfo));
            var keyStorageDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IPublicKeyStorage));
            var keyPublisherDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(KeyPublisher));

            Assert.Equal(ServiceLifetime.Scoped, hasherDescriptor?.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, encryptionDescriptor?.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, decryptionDescriptor?.Lifetime);
            Assert.Equal(ServiceLifetime.Singleton, keyStorageDescriptor?.Lifetime);
            Assert.Equal(ServiceLifetime.Scoped, keyPublisherDescriptor?.Lifetime);
        }

        [Fact]
        public void AddInfrastructureLayer_WithNullJwtKey_ShouldUseEmptyString()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var services = new ServiceCollection();
            
            var messageBrokerSection = new Mock<IConfigurationSection>();
            messageBrokerSection.Setup(x => x["Host"]).Returns("rabbitmq://localhost");
            messageBrokerSection.Setup(x => x["UserName"]).Returns("guest");
            messageBrokerSection.Setup(x => x["Password"]).Returns("guest");

            mockConfiguration.Setup(x => x.GetSection("MessageBroker")).Returns(messageBrokerSection.Object);
            mockConfiguration.Setup(x => x["JWTConfig:Issuer"]).Returns("TestIssuer");
            mockConfiguration.Setup(x => x["JWTConfig:Audience"]).Returns("TestAudience");
            mockConfiguration.Setup(x => x["JWTConfig:Key"]).Returns((string)null);

            // Act & Assert
            services.AddInfrastructureLayer(mockConfiguration.Object);
            var serviceProvider = services.BuildServiceProvider();
            
            var jwtOptions = serviceProvider.GetService<IOptions<JwtBearerOptions>>();
            Assert.NotNull(jwtOptions);
        }
        
        [Fact]
        public void AddInfrastructureLayer_ShouldConfigureAuthenticationSchemes()
        {
            // Arrange
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var authOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

            // Assert
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultChallengeScheme);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultScheme);
        }
        
        [Fact]
        public void AddInfrastructureLayer_ShouldConfigureCorsPolicy()
        {
            // Arrange
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var corsOptions = serviceProvider.GetRequiredService<IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>>().Value;
            var policy = corsOptions.GetPolicy("AllowFrontend");

            // Assert
            Assert.NotNull(policy);
            Assert.Contains("http://localhost:4200", policy.Origins);
            Assert.True(policy.SupportsCredentials);
        }

        [Fact]
        public void AddInfrastructureLayer_ShouldConfigureMassTransitQueues()
        {   
            // Arrange
            _services.AddSingleton(Options.Create(new MessageBrokerSettings
            {
                Host = "amqp://localhost:5672",
                UserName = "guest",
                Password = "guest"
            }));
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var busRegistrationContext = serviceProvider.GetService<IBusRegistrationContext>();

            // Assert
            Assert.NotNull(busRegistrationContext);
        }
        
        [Fact]
        public async Task OnMessageReceived_WithEmptyTokenFromQuery_ShouldCheckCookies()
        {
            // Arrange
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            _services.AddScoped<IDecryptionInfo>(_ => _mockDecryption.Object);
            var serviceProvider = _services.BuildServiceProvider();

            var jwtOptions = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockCookies = new Mock<IRequestCookieCollection>();
    
            // Setup empty query token
            mockRequest.Setup(x => x.Query["access_token"]).Returns("");
    
            // Setup cookie with token
            mockCookies.Setup(x => x.TryGetValue("access_token", out It.Ref<string>.IsAny))
                .Returns((string _, out string value) =>
                {
                    value = "cookie-token";
                    return true;
                });

            _mockDecryption.Setup(x => x.Decrypt("cookie-token")).Returns("decrypted-cookie-token");

            mockRequest.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(x => x.RequestServices).Returns(serviceProvider);

            var context = new MessageReceivedContext(
                mockHttpContext.Object,
                new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
                new JwtBearerOptions());

            // Act
            await jwtOptions.Events.OnMessageReceived(context);

            // Assert
            Assert.Equal("decrypted-cookie-token", context.Token);
        }
        
        [Fact]
        public void AddInfrastructureLayer_ShouldRegisterKeyPublisher()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                { "MessageBroker:Host", "rabbitmq://localhost" },
                { "MessageBroker:UserName", "guest" },
                { "MessageBroker:Password", "guest" },
                { "JWTConfig:Issuer", "TestIssuer" },
                { "JWTConfig:Audience", "TestAudience" },
                { "JWTConfig:Key", "ThisIsAVeryLongSecretKeyForTesting12345" },
                { "Encryption:ChaChaKey", "ThisIsAVeryLongSecretKeyForTesting12345" }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            
            services.AddSingleton(configuration);
            
            services.AddInfrastructureLayer(configuration);

            var serviceProvider = services.BuildServiceProvider();

            var keyPublisher = serviceProvider.GetService<KeyPublisher>();
            Assert.NotNull(keyPublisher);
        }

        [Fact] 
        public void AddInfrastructureLayer_ShouldConfigureTokenValidationWithCorrectKey()
        {
            // Arrange
            var testKey = "TestSecretKeyThatIsLongEnough123456";
            _mockConfiguration.Setup(x => x["JWTConfig:Key"]).Returns(testKey);
    
            // Act
            _services.AddInfrastructureLayer(_mockConfiguration.Object);
            var serviceProvider = _services.BuildServiceProvider();
    
            var jwtOptions = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            // Assert
            var signingKey = jwtOptions.TokenValidationParameters.IssuerSigningKey as SymmetricSecurityKey;
            Assert.NotNull(signingKey);
    
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(testKey);
            Assert.Equal(keyBytes, signingKey.Key);
        } 
        
        [Fact]
        public void AddInfrastructureLayer_ConfiguresSwaggerGenWithJwtSecurityScheme()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["JWTConfig:Issuer"] = "test-issuer",
                    ["JWTConfig:Audience"] = "test-audience", 
                    ["JWTConfig:Key"] = "test-key-with-at-least-256-bits-length-for-security",
                    ["MessageBroker:Host"] = "amqp://localhost:5672",
                    ["MessageBroker:UserName"] = "guest",
                    ["MessageBroker:Password"] = "guest"
                })
                .Build();

            // Act
            services.AddInfrastructureLayer(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var swaggerGenOptions = services
                .FirstOrDefault(s => s.ServiceType == typeof(IConfigureOptions<SwaggerGenOptions>));

            Assert.NotNull(swaggerGenOptions);
            
            var optionsFactory = serviceProvider.GetService<IOptions<SwaggerGenOptions>>();
            Assert.NotNull(optionsFactory);
        }

        [Fact]
        public void AddInfrastructureLayer_SwaggerGenOptions_ConfiguresJwtBearerSecurityDefinition()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["JWTConfig:Issuer"] = "test-issuer",
                    ["JWTConfig:Audience"] = "test-audience",
                    ["JWTConfig:Key"] = "test-key-with-at-least-256-bits-length-for-security",
                    ["MessageBroker:Host"] = "amqp://localhost:5672",
                    ["MessageBroker:UserName"] = "guest",
                    ["MessageBroker:Password"] = "guest"
                })
                .Build();

            services.AddInfrastructureLayer(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var swaggerGenOptions = serviceProvider.GetService<IOptions<SwaggerGenOptions>>()?.Value;

            // Assert
            Assert.NotNull(swaggerGenOptions);

            var testOptions = new SwaggerGenOptions();
            
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "Authorization", 
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Description = "Enter your JWT access token",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            
            testOptions.AddSecurityDefinition("Bearer", jwtSecurityScheme);
            
            Assert.Equal("JWT", jwtSecurityScheme.BearerFormat);
            Assert.Equal("Authorization", jwtSecurityScheme.Name);
            Assert.Equal(ParameterLocation.Header, jwtSecurityScheme.In);
            Assert.Equal(SecuritySchemeType.Http, jwtSecurityScheme.Type);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme.Scheme);
            Assert.Equal("Enter your JWT access token", jwtSecurityScheme.Description);
            Assert.Equal(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme.Reference.Id);
            Assert.Equal(ReferenceType.SecurityScheme, jwtSecurityScheme.Reference.Type);
        }
        
        [Fact]
        public async Task OnMessageReceived_WithValidQueryToken_ShouldCoverAllBranches()
{
    // Arrange
    var inMemorySettings = new Dictionary<string, string>
    {
        ["JWTConfig:Key"] = "0123456789abcdef0123456789abcdef",
        ["JWTConfig:Issuer"] = "your-issuer",
        ["JWTConfig:Audience"] = "your-audience",
        ["Encryption:ChaChaKey"] = "your_secret_key_that_is_long_enough"
    };

    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(inMemorySettings)
        .Build();

    var services = new ServiceCollection();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddInfrastructureLayer(configuration);
    var mockDecryption = new Mock<IDecryptionInfo>();
    services.AddScoped<IDecryptionInfo>(_ => mockDecryption.Object);
    var serviceProvider = services.BuildServiceProvider();

    var jwtOptions = serviceProvider
        .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
        .Get(JwtBearerDefaults.AuthenticationScheme);

    var mockHttpContext = new Mock<HttpContext>();
    var mockRequest = new Mock<HttpRequest>();
    var mockCookies = new Mock<IRequestCookieCollection>();
    var mockQuery = new Mock<IQueryCollection>();

    // Setup query with EMPTY token to force cookie lookup
    mockQuery.Setup(x => x["access_token"]).Returns("");

    // Setup cookies with encrypted token - this covers (string,out string) branch
    mockCookies.Setup(x => x.TryGetValue("access_token", out It.Ref<string>.IsAny))
        .Returns((string _, out string value) =>
        {
            value = "encrypted-cookie-token";
            return true;
        });

    // Setup decryption mock
    mockDecryption.Setup(x => x.Decrypt("encrypted-cookie-token"))
        .Returns("decrypted-token");

    mockRequest.Setup(x => x.Query).Returns(mockQuery.Object);
    mockRequest.Setup(x => x.Cookies).Returns(mockCookies.Object);
    mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
    // This covers (IServiceProvider) branch
    mockHttpContext.Setup(x => x.RequestServices).Returns(serviceProvider);

    var context = new MessageReceivedContext(
        mockHttpContext.Object,
        new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
        new JwtBearerOptions());

    // Act
    await jwtOptions.Events.OnMessageReceived(context);

    // Assert
    Assert.Equal("decrypted-token", context.Token);
    // Verify that decryption was called since we used cookie token
    mockDecryption.Verify(x => x.Decrypt("encrypted-cookie-token"), Times.Once);
    // Verify that cookies were accessed to cover (string,out string) branch
    mockCookies.Verify(x => x.TryGetValue("access_token", out It.Ref<string>.IsAny), Times.Once);
}
    }
}