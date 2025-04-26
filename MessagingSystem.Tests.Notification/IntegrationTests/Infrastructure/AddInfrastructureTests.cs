using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.Services.Notification.Infrastructure;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;
using MessagingSystem.Services.Notification.Infrastructure.MessageBroker;
using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessagingSystem.Tests.Notification.IntegrationTests.Infrastructure;

public class AddInfrastructureTests
{
    private readonly IServiceProvider _serviceProvider;

    public AddInfrastructureTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();
        
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(env => env.ContentRootPath).Returns(Directory.GetCurrentDirectory());
        services.AddSingleton(mockEnv.Object);
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        
        services.AddInfrastructureLayer(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Should_Resolve_IEmailSender()
    {
        var service = _serviceProvider.GetService<IEmailSender>();
        Assert.NotNull(service);
        Assert.IsType<EmailSender>(service);
    }

    [Fact]
    public void Should_Resolve_ITemplateReader()
    {
        var service = _serviceProvider.GetService<ITemplateReader>();
        Assert.NotNull(service);
        Assert.IsType<TemplateReader>(service);
    }

    [Fact]
    public void Should_Resolve_IEncryptionInfo()
    {
        var service = _serviceProvider.GetService<IEncryptionInfo>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Should_Resolve_IDecryptionInfo()
    {
        var service = _serviceProvider.GetService<IDecryptionInfo>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Should_Resolve_MessageBrokerSettings()
    {
        var settings = _serviceProvider.GetService<MessageBrokerSettings>();
        Assert.NotNull(settings);
        Assert.Equal("rabbitmq://localhost", settings.Host);
    }

    [Fact]
    public void Should_Configure_MassTransit()
    {
        var bus = _serviceProvider.GetService<IBusControl>();
        Assert.NotNull(bus);
    }
}
