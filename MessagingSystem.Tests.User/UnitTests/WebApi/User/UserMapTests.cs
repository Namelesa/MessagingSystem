using AutoMapper;
using FluentAssertions;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.WebApi.User;
using MessagingSystem.Services.User.WebApi.User.Contracts;

namespace MessagingSystem.Tests.User.UnitTests.WebApi.User
{
    public class UserMapTests
    {
        private readonly IMapper _mapper;

        public UserMapTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UserMap>();
            });

            config.AssertConfigurationIsValid();
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void EditUserContract_Should_Map_To_UserDto()
        {
            // Arrange
            var contract = new EditUserContract(
                firstName: "John",
                lastName: "Doe",
                login: "john_doe@",
                email: "john@example.com",
                nickName: "j_doe@"
            );

            // Act
            var userDto = _mapper.Map<UserDto>(contract);

            // Assert
            userDto.FirstName.Should().Be(contract.FirstName);
            userDto.LastName.Should().Be(contract.LastName);
            userDto.Login.Should().Be(contract.Login);
            userDto.Email.Should().Be(contract.Email);
            userDto.NickName.Should().Be(contract.NickName);
        }

        [Fact]
        public void UserDto_Should_Map_To_User_With_Expected_Values()
        {
            // Arrange
            var userDto = new UserDto(
                firstName: "John",
                lastName: "Doe",
                login: "john_doe@",
                email: "john@example.com",
                nickName: "j_doe@",
                image: "test"
            );

            // Act
            var user = _mapper.Map<Services.User.Core.User.User>(userDto);

            // Assert
            user.UserName.Should().Be("JohnDoe");
            user.NormalizedUserName.Should().Be("JOHNDOE");
            user.NormalizedEmail.Should().Be("JOHN@EXAMPLE.COM");
            user.Email.Should().Be(userDto.Email);
            user.Login.Should().Be(userDto.Login);
            user.NickName.Should().Be(userDto.NickName);
        }

        [Fact]
        public void UserDto_Should_Map_To_User_And_Set_Hashes()
        {
            // Arrange
            var userDto = new UserDto(
                firstName: "John",
                lastName: "Doe",
                login: "john_doe@",
                email: "john@example.com",
                nickName: "j_doe@",
                image: "test"
            );

            var user = _mapper.Map<Services.User.Core.User.User>(userDto);

            // Act
            user.SetHashes("loginHash", "emailHash", "nickNameHash");

            // Assert
            user.HashLogin.Should().Be("loginHash");
            user.HashEmail.Should().Be("emailHash");
            user.HashNickName.Should().Be("nickNameHash");
        }
    }
}
