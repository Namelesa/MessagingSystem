using AutoMapper;
using Encryptor.Encryption;
using FluentAssertions;
using MassTransit;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;
using MessagingSystem.Services.User.Infrastructure.MessageBroker;
using MessagingSystem.Services.User.Infrastructure.PasswordHasher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace MessagingSystem.Tests.User.UnitTests.Application
{
    public class AddApplicationTests
    {
        [Fact]
        public void AddApplicationLayer_RegistersAllDependenciesCorrectly()
        {
            var services = new ServiceCollection();
            
            var userRepositoryMock = new Mock<IUserRepository>();
            
            userRepositoryMock.Setup(repo => repo.FindUserByHashLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new Services.User.Core.User.User("","", ""));
            userRepositoryMock.Setup(repo => repo.FindUserByHashNickNameAsync(It.IsAny<string>()))
                .ReturnsAsync(new Services.User.Core.User.User("","", ""));
            userRepositoryMock.Setup(repo => repo.FindUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Services.User.Core.User.User("","", ""));
            
            var hasherPasswordMock = new Mock<IHasherPassword>();
            var encryptionInfoMock = new Mock<IEncryptionInfo>();
            var mapperMock = new Mock<IMapper>();
            var hasherMock = new Mock<IHasher>();
            var publishEndpointMock = new Mock<IPublishEndpoint>();
            var publicKeyStorageMock = new Mock<IPublicKeyStorage>();
            
            var configDict = new Dictionary<string, string>
            {
                ["MessageBrokerSettings:Host"] = "amqp://localhost",
                ["MessageBrokerSettings:UserName"] = "guest",
                ["MessageBrokerSettings:Password"] = "guest"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict!)
                .Build();
            
            services.AddScoped<IUserRepository>(_ => userRepositoryMock.Object);
            services.AddScoped<IHasherPassword>(_ => hasherPasswordMock.Object);
            services.AddScoped<IEncryptionInfo>(_ => encryptionInfoMock.Object);
            services.AddScoped<IMapper>(_ => mapperMock.Object);
            services.AddScoped<IHasher>(_ => hasherMock.Object);
            services.AddScoped<IPublishEndpoint>(_ => publishEndpointMock.Object);
            services.AddScoped<IPublicKeyStorage>(_ => publicKeyStorageMock.Object);
            
            services.AddApplicationLayer(configuration);
            var provider = services.BuildServiceProvider();

            // Act
            var registerOrchestrator = provider.GetRequiredService<IRegisterOrchestrator>();

            // Assert
            registerOrchestrator.Should().NotBeNull();
            
            var settings = provider.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;
            settings.Host.Should().Be("amqp://localhost");
            settings.UserName.Should().Be("guest");
            settings.Password.Should().Be("guest");
        }
    }
}
