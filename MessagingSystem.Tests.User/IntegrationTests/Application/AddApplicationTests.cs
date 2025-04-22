using Docker.DotNet;
using Docker.DotNet.Models;
using MassTransit;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessagingSystem.Tests.User.IntegrationTests.Application;

public class ApplicationLayerIntegrationTests : IAsyncLifetime
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private ServiceProvider _serviceProvider = null!;
    private readonly RabbitMqContainerManager _rabbitMqManager = new();

    public async Task InitializeAsync()
    {
        // Запускаем контейнер RabbitMQ
        await _rabbitMqManager.StartAsync();

        // Настройка конфигурации
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("MessageBrokerSettings:Host", "amqp://localhost:5672"),
                new KeyValuePair<string, string>("MessageBrokerSettings:UserName", "guest"),
                new KeyValuePair<string, string>("MessageBrokerSettings:Password", "guest")
            })
            .Build();

        // Регистрация зависимостей
        _services.AddApplicationLayer(configuration);
        _serviceProvider = _services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        // Удаляем контейнер RabbitMQ
        await _rabbitMqManager.StopAsync();
        _serviceProvider.Dispose();
    }

    [Fact]
    public void Should_Register_All_Dependencies()
    {
        // Act & Assert
        Assert.NotNull(_serviceProvider.GetService<IRegisterOrchestrator>());
        Assert.NotNull(_serviceProvider.GetService<ILoginOrchestrator>());
        Assert.NotNull(_serviceProvider.GetService<IUserOrchestrator>());
        Assert.NotNull(_serviceProvider.GetService<IBusControl>()); // Проверяем, что MassTransit зарегистрирован
    }

    [Fact]
    public async Task Should_Consume_Message_Through_PublicKeyQueue()
    {
        // Arrange
        var bus = _serviceProvider.GetRequiredService<IBusControl>();
        await bus.StartAsync();

        var consumeContextTaskCompletionSource = new TaskCompletionSource<string>();

        // Подписываемся на событие получения сообщения
        bus.ConnectReceiveEndpoint("public-key-notification-queue", e =>
        {
            e.Handler<string>(context =>
            {
                consumeContextTaskCompletionSource.TrySetResult(context.Message);
                return Task.CompletedTask;
            });
        });

        // Act
        const string testMessage = "TestMessage";
        await bus.Publish(testMessage);

        // Assert
        var consumedMessage = await consumeContextTaskCompletionSource.Task;
        Assert.Equal(testMessage, consumedMessage);

        await bus.StopAsync();
    }
}

public class RabbitMqContainerManager : IDisposable
{
    private readonly DockerClient _dockerClient;
    private string? _containerId;

    public RabbitMqContainerManager()
    {
        // Подключаемся к Docker Daemon
        _dockerClient = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")) // Для Linux/Mac
            .CreateClient();
    }

    public async Task StartAsync()
    {
        // Проверяем, что образ RabbitMQ существует
        var images = await _dockerClient.Images.ListImagesAsync(new ImagesListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "reference", new Dictionary<string, bool> { { "rabbitmq:3-management", true } } }
            }
        });

        if (images.Count == 0)
        {
            // Если образа нет, загружаем его
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = "rabbitmq", Tag = "3-management" },
                null,
                new Progress<JSONMessage>());
        }

        // Создаем контейнер
        var createResponse = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = "rabbitmq:3-management",
            Name = "rabbitMqTests",
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "5672/tcp", new List<PortBinding> { new PortBinding { HostPort = "5672" } } },
                    { "15672/tcp", new List<PortBinding> { new PortBinding { HostPort = "15672" } } }
                }
            }
        });

        _containerId = createResponse.ID;

        // Запускаем контейнер
        await _dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());
    }

    public async Task StopAsync()
    {
        if (_containerId == null)
        {
            return;
        }

        // Останавливаем контейнер
        await _dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters());

        // Удаляем контейнер
        await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters { Force = true });
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}