using AutoMapper;
using FluentAssertions;
using MessagingSystem.Services.User.Application.Auth.Register.Dto;
using MessagingSystem.Services.User.WebApi.Register;
using MessagingSystem.Services.User.WebApi.Register.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.Register
{
    public class RegisterMapTests
    {
        private readonly IMapper _mapper;

        public RegisterMapTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<RegisterMap>();
            });

            config.AssertConfigurationIsValid();
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void RegisterContract_Should_Map_To_RegisterDto()
        {
            // Arrange
            var contract = new RegisterContract(
                firstName: "John",
                lastName: "Doe",
                login: "john_doe@",
                email: "john@example.com",
                nickName: "j_doe@",
                password: "Passw0rd@",
                image: "test"
            );

            // Act
            var dto = _mapper.Map<RegisterDto>(contract);

            // Assert
            dto.FirstName.Should().Be(contract.FirstName);
            dto.LastName.Should().Be(contract.LastName);
            dto.Login.Should().Be(contract.Login);
            dto.Email.Should().Be(contract.Email);
            dto.NickName.Should().Be(contract.NickName);
            dto.Password.Should().Be(contract.Password);
        }

        [Fact]
        public void RegisterDto_Should_Map_To_User_With_Expected_Values()
        {
            // Arrange
            var dto = new RegisterDto(
                email: "test@example.com",
                login: "test_login@",
                firstName: "Alice",
                lastName: "Smith",
                nickName: "a_smith@",
                password: "P@ssw0rd",
                image: "test"
            );

            // Act
            var user = _mapper.Map<Services.User.Core.User.User>(dto);

            // Assert
            user.UserName.Should().Be("AliceSmith");
            user.NormalizedUserName.Should().Be("ALICESMITH");
            user.NormalizedEmail.Should().Be("TEST@EXAMPLE.COM");
            user.EmailConfirmed.Should().BeFalse();
            user.PasswordHash.Should().Be(dto.Password);
            user.SecurityStamp.Should().NotBeNullOrEmpty();
        }
    }
}
