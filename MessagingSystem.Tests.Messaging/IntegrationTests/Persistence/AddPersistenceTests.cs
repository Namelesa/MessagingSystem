using FluentAssertions;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Core.Oto.Users;
using MessagingSystem.Services.Messaging.Persistence;
using MessagingSystem.Services.Messaging.Persistence.Group;
using MessagingSystem.Services.Messaging.Persistence.Group.GroupDbInitializer;
using MessagingSystem.Services.Messaging.Persistence.Oto;
using MessagingSystem.Services.Messaging.Persistence.Oto.OtoDbInitializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessagingSystem.Tests.Messaging.IntegrationTests.Persistence;

public class AddPersistenceIntegrationTests : IDisposable
{
    private readonly ServiceCollection _services;
    private readonly IConfiguration _configuration;
    private ServiceProvider _serviceProvider = null!;

    public AddPersistenceIntegrationTests()
    {
        _services = new ServiceCollection();
        
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", 
                "Host=localhost;Port=5432;Database=MessagingSystemOtoTest;Username=test;Password=test"),
            new KeyValuePair<string, string>("ConnectionStrings:GroupDefaultConnection", 
                "Host=localhost;Port=5432;Database=MessagingSystemGroupTest;Username=test;Password=test")
        }!);
        _configuration = configurationBuilder.Build();
    }

    [Fact]
    public void AddPersistenceLayer_ShouldRegisterAllRequiredServices()
    {
        // Act
        _services.AddLogging();
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        _serviceProvider.GetService<OtoAppDbContext>().Should().NotBeNull();
        _serviceProvider.GetService<GroupAppDbContext>().Should().NotBeNull();

        // Assert 
        _serviceProvider.GetService<IDbContextFactory<OtoAppDbContext>>().Should().NotBeNull();
        _serviceProvider.GetService<IDbContextFactory<GroupAppDbContext>>().Should().NotBeNull();
        
        _serviceProvider.GetService<IOtoMessageRepository>().Should().NotBeNull();
        _serviceProvider.GetService<IGroupInfoRepository>().Should().NotBeNull();
        _serviceProvider.GetService<IChatRepository>().Should().NotBeNull();
        _serviceProvider.GetService<IGroupMembersRepository>().Should().NotBeNull();
        _serviceProvider.GetService<IGroupMessagesRepository>().Should().NotBeNull();
        _serviceProvider.GetService<IUserImageRepository>().Should().NotBeNull();

        // Assert
        _serviceProvider.GetService<IOtoDbInitializer>().Should().NotBeNull();
        _serviceProvider.GetService<IGroupDbInitializer>().Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceLayer_ShouldConfigureOtoDbContextWithCorrectConnectionString()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        using var scope = _serviceProvider.CreateScope();
        var otoContext = scope.ServiceProvider.GetRequiredService<OtoAppDbContext>();
        
        var connectionString = otoContext.Database.GetConnectionString();
        connectionString.Should().Contain("MessagingSystemOtoTest");
    }

    [Fact]
    public void AddPersistenceLayer_ShouldConfigureGroupDbContextWithCorrectConnectionString()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        using var scope = _serviceProvider.CreateScope();
        var groupContext = scope.ServiceProvider.GetRequiredService<GroupAppDbContext>();
        
        var connectionString = groupContext.Database.GetConnectionString();
        connectionString.Should().Contain("MessagingSystemGroupTest");
    }

    [Fact]
    public void AddPersistenceLayer_ShouldRegisterDbContextFactoriesAsSingleton()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        var factory1 = _serviceProvider.GetService<IDbContextFactory<OtoAppDbContext>>();
        var factory2 = _serviceProvider.GetService<IDbContextFactory<OtoAppDbContext>>();
        
        factory1.Should().BeSameAs(factory2);

        var groupFactory1 = _serviceProvider.GetService<IDbContextFactory<GroupAppDbContext>>();
        var groupFactory2 = _serviceProvider.GetService<IDbContextFactory<GroupAppDbContext>>();
        
        groupFactory1.Should().BeSameAs(groupFactory2);
    }

    [Fact]
    public void AddPersistenceLayer_ShouldRegisterRepositoriesAsScoped()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var repo1Scope1 = scope1.ServiceProvider.GetService<IOtoMessageRepository>();
        var repo2Scope1 = scope1.ServiceProvider.GetService<IOtoMessageRepository>();
        var repo1Scope2 = scope2.ServiceProvider.GetService<IOtoMessageRepository>();
        
        repo1Scope1.Should().BeSameAs(repo2Scope1);
        
        repo1Scope1.Should().NotBeSameAs(repo1Scope2);
    }

    [Fact]
    public void AddPersistenceLayer_DbContextFactoryShouldCreateWorkingContexts()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        var otoFactory = _serviceProvider.GetService<IDbContextFactory<OtoAppDbContext>>();
        var groupFactory = _serviceProvider.GetService<IDbContextFactory<GroupAppDbContext>>();

        using var otoContext = otoFactory.CreateDbContext();
        using var groupContext = groupFactory.CreateDbContext();

        otoContext.Should().NotBeNull();
        groupContext.Should().NotBeNull();
        
        otoContext.Database.GetConnectionString().Should().Contain("MessagingSystemOtoTest");
        groupContext.Database.GetConnectionString().Should().Contain("MessagingSystemGroupTest");
    }
    
    [Fact]
    public void AddPersistenceLayer_ShouldRegisterAllRepositoryTypes()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert 
        var serviceDescriptors = _services.Where(s => s.ServiceType.Name.EndsWith("Repository")).ToList();
        
        serviceDescriptors.Should().HaveCount(6);
        serviceDescriptors.Should().OnlyContain(s => s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPersistenceLayer_ShouldAllowMultipleDbContextInstances()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        using var scope = _serviceProvider.CreateScope();
        var context1 = scope.ServiceProvider.GetRequiredService<OtoAppDbContext>();
        var context2 = scope.ServiceProvider.GetRequiredService<OtoAppDbContext>();
        
        context1.Should().BeSameAs(context2);
    }

    [Fact]
    public void AddPersistenceLayer_DbContextsShouldBeConfiguredForPostgreSQL()
    {
        // Act
        _services.AddPersistenceLayer(_configuration);
        _serviceProvider = _services.BuildServiceProvider();

        // Assert
        using var scope = _serviceProvider.CreateScope();
        var otoContext = scope.ServiceProvider.GetRequiredService<OtoAppDbContext>();
        var groupContext = scope.ServiceProvider.GetRequiredService<GroupAppDbContext>();

        otoContext.Database.IsNpgsql().Should().BeTrue();
        groupContext.Database.IsNpgsql().Should().BeTrue();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}