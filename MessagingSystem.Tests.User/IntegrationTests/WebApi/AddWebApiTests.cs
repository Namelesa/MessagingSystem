using AutoMapper;
using MessagingSystem.Services.User.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessagingSystem.Tests.User.IntegrationTests.WebApi;

public class AddWebApiTests
{
    private readonly IServiceProvider _serviceProvider;

    public AddWebApiTests()
    {
        var serviceCollection = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build(); 

        serviceCollection.AddWebApiLayer(configuration);

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void AddWebApiLayer_Should_Register_AutoMapper_Profiles()
    {
        var mapper = _serviceProvider.GetService<IMapper>();
        Assert.NotNull(mapper);

        var configurationProvider = mapper.ConfigurationProvider;
        
        configurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void AddWebApiLayer_Should_Throw_Exception_If_Profiles_Are_Not_Registered()
    {
        var mapper = _serviceProvider.GetService<IMapper>();
        var configurationProvider = mapper?.ConfigurationProvider;
        
        var exception = Record.Exception(() => configurationProvider?.AssertConfigurationIsValid());

        Assert.Null(exception);
    }
}