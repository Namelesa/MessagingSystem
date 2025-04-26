using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MessagingSystem.Services.User.Application;
using MessagingSystem.Services.User.Application.Auth.Login;
using MessagingSystem.Services.User.Application.Auth.Register;
using MessagingSystem.Services.User.Application.Messaging.Key;
using MessagingSystem.Services.User.Infrastructure.MessageBroker;
using FluentValidation;
using MessagingSystem.Services.User.Application.Auth.Login.Dto;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;

namespace MessagingSystem.Tests.User.IntegrationTests.Application
{
    public class AddApplicationTests
    {
        private readonly ServiceProvider _serviceProvider;

        public AddApplicationTests()
        {
            // Сначала создаем сервисы
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Добавляем приложение с настройками
            services.AddApplicationLayer(configuration);

            // Строим DI контейнер
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void Should_Configure_MassTransit_And_Consumers()
        {
            // Получаем объект из контейнера DI
            var busControl = _serviceProvider.GetService<IBusControl>();
            var consumer = _serviceProvider.GetService<PublicKeyConsumer>();

            // Проверяем, что busControl и consumer не равны null
            busControl.Should().NotBeNull();
            consumer.Should().NotBeNull();
        }

        [Fact]
        public void Should_Register_Services_In_DI()
        {
            // Проверяем, что все нужные сервисы зарегистрированы
            var registerOrchestrator = _serviceProvider.GetService<IRegisterOrchestrator>();
            var loginOrchestrator = _serviceProvider.GetService<ILoginOrchestrator>();
            var userOrchestrator = _serviceProvider.GetService<IUserOrchestrator>();

            registerOrchestrator.Should().NotBeNull();
            loginOrchestrator.Should().NotBeNull();
            userOrchestrator.Should().NotBeNull();
        }

        [Fact]
        public void Should_Register_Validators_In_DI()
        {
            // Проверяем регистрацию валидаторов
            var registerValidator = _serviceProvider.GetService<IValidator<RegisterDto>>();
            var loginValidator = _serviceProvider.GetService<IValidator<LoginDto>>();
            var userValidator = _serviceProvider.GetService<IValidator<UserDto>>();

            registerValidator.Should().NotBeNull();
            loginValidator.Should().NotBeNull();
            userValidator.Should().NotBeNull();
        }

        [Fact]
        public void Should_Configure_MessageBroker_Settings_From_Configuration()
        {
            // Проверяем, что MessageBrokerSettings настроены
            var settings = _serviceProvider.GetService<MessageBrokerSettings>();

            settings.Should().NotBeNull();
            settings.Host.Should().NotBeNullOrEmpty();
            settings.UserName.Should().NotBeNullOrEmpty();
            settings.Password.Should().NotBeNullOrEmpty();
        }
    }
}
