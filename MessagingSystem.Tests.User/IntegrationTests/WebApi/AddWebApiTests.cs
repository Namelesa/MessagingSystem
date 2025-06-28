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
    public void AddWebApiLayer_Should_Resolve_IMapper()
    {
        var mapper = _serviceProvider.GetService<IMapper>();
        Assert.NotNull(mapper);
    }

    [Fact]
    public void AddWebApiLayer_Should_Map_BasicObject_WithoutExceptions()
    {
        var mapper = _serviceProvider.GetRequiredService<IMapper>();

        var contract = new MessagingSystem.Services.User.WebApi.User.Contracts.EditUserContract
        {
            FirstName = "John",
            LastName = "Doe",
            Login = "johndoe",
            Email = "john@example.com",
            NickName = "johnny"
        };

        var dto = mapper.Map<MessagingSystem.Services.User.Application.User.Dto.UserDto>(contract);
        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("johndoe", dto.Login);
    }
}