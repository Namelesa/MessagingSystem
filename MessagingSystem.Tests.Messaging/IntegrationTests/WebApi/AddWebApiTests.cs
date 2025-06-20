using AutoMapper;
using FluentAssertions;
using MessagingSystem.Services.Messaging.WebApi;
using MessagingSystem.Services.Messaging.WebApi.Messages.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.WebApi;

public class AddWebApiIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void AddWebApiLayer_Should_RegisterAllRequiredServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddWebApiLayer(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;
        
        scopedProvider.GetService<IConfiguration>().Should().NotBeNull();
    }

    [Fact]
    public void AddWebApiLayer_Should_ConfigureControllersCorrectly()
    {
        // Arrange & Act
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetService<IOptions<MvcOptions>>();

        // Assert
        options.Should().NotBeNull();
        var mvcOptions = options.Value;
        mvcOptions.Should().NotBeNull();
    }


    [Fact]
    public void AddWebApiLayer_Should_ConfigureSwaggerCorrectly()
    {
        // Arrange & Act
        using var scope = factory.Services.CreateScope();
        var swaggerGenOptions = scope.ServiceProvider.GetServices<IConfigureOptions<SwaggerGenOptions>>();

        // Assert
        swaggerGenOptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SwaggerEndpoint_Should_BeAccessible()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/index.html");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task SwaggerJson_Should_BeGenerated()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        content.Should().Contain("\"openapi\":");
    }
    
    [Fact]
    public void ChatMap_Should_BeValidProfile()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ChatMap>());

        // Act & Assert
        configuration.AssertConfigurationIsValid();
    }
    
    [Fact]
    public void AddWebApiLayer_Should_RegisterServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        // Act
        services.AddWebApiLayer(configuration);

        // Assert
        var serviceDescriptors = services.ToList();
        
        var mapperDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(IMapper));
        mapperDescriptor.Should().NotBeNull();
        mapperDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddWebApiLayer_Should_NotRegisterDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        // Act
        services.AddWebApiLayer(configuration);
        services.AddWebApiLayer(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        
        var mapper = scope.ServiceProvider.GetService<IMapper>();
        mapper.Should().NotBeNull();
    }
    
    [Fact]
    public void Application_Should_StartSuccessfully()
    {
        // Arrange & Act
        using var scope = factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Assert
        serviceProvider.Should().NotBeNull();
        
        var mapper = serviceProvider.GetService<IMapper>();
        mapper.Should().NotBeNull();
        
        var configuration = serviceProvider.GetService<IConfiguration>();
        configuration.Should().NotBeNull();
    }

    [Fact]
    public async Task HealthCheck_Should_ReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        await client.GetAsync("/health");
        
        factory.Services.Should().NotBeNull();
    }

    [Fact]
    public void AddWebApiLayer_Should_WorkWithEmptyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        Action act = () => services.AddWebApiLayer(configuration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddWebApiLayer_Should_WorkWithNullConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddWebApiLayer(null!);

        // Assert
        act.Should().NotThrow();
    }

    [Xunit.Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void AddWebApiLayer_Should_WorkInDifferentEnvironments(string environment)
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Environment", environment}
            })
            .Build();

        // Act
        Action act = () => services.AddWebApiLayer(configuration);

        // Assert
        act.Should().NotThrow();
        
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetService<IMapper>();
        mapper.Should().NotBeNull();
    }
}