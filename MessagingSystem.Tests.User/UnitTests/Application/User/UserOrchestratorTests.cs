using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using FluentValidation.Results;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.Keys;
using Moq;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace MessagingSystem.Tests.User.UnitTests.Application.User;

    public class UserOrchestratorTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<UserDto>> _validatorMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IEncryptionInfo> _encryptInfoMock;
        private readonly Mock<IDecryptionInfo> _decncryptInfoMock;
        private readonly Mock<IHasher> _hasherMock;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<IPublicKeyStorage> _publicKeyStorageMock;
        private readonly UserOrchestrator _orchestrator;
        private readonly Mock<IRequestClient<EditUserInfoRequest>> _client;

        private const string UserId = "user123";
        private const string PublicKey = "test-public-key";
        private const string HashedValue = "hashed-value";

        public UserOrchestratorTests()
        {
            _mapperMock = new Mock<IMapper>();
            _validatorMock = new Mock<IValidator<UserDto>>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _encryptInfoMock = new Mock<IEncryptionInfo>();
            _decncryptInfoMock = new Mock<IDecryptionInfo>();
            _hasherMock = new Mock<IHasher>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _publicKeyStorageMock = new Mock<IPublicKeyStorage>();
            _client = new Mock<IRequestClient<EditUserInfoRequest>>();

            _orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                _decncryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _client.Object,
                _publicKeyStorageMock.Object
                );
            
            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            
            _hasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns(HashedValue);
        }

        #region EditUserInfoAsync Tests

        [Fact]
        public async Task EditUserInfoAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Update user info", result.Data);
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(existingUser), Times.Once);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Once);
            _encryptInfoMock.Verify(
                x => x.EncryptObjectStringsForUpdate(existingUser), 
                Times.Once);
            _encryptInfoMock.Verify(
                x => x.EncryptRsaObjectStrings(It.IsAny<EditUserEmail>(), PublicKey), 
                Times.Once);
            _hasherMock.Verify(x => x.Hash(userDto.Login), Times.Once);
            _hasherMock.Verify(x => x.Hash(userDto.Email), Times.Once);
            _hasherMock.Verify(x => x.Hash(userDto.NickName), Times.Once);
        }

        [Fact]
        public async Task EditUserInfoAsync_WithInvalidDto_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var validationErrors = new List<ValidationFailure> { new("Login", "Invalid login") };
            
            SetupValidationFailure(userDto, validationErrors);

            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid login", result.Message);
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task EditUserInfoAsync_WithNonExistingUser_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            
            SetupValidationSuccess(userDto);
            SetupUserNotFound(UserId);

            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task EditUserInfoAsync_WithNullPublicKey_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();
            
            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            
            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns((string)null);

            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
            
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task EditUserInfoAsync_WithNullUserProperties_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();
            existingUser.Email = null;
            
            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User can not have null properties", result.Message);
            
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task EditUserInfoAsync_WithRepositoryException_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();
            var exception = new Exception("Database error");
            
            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);
            
            _userRepositoryMock
                .Setup(x => x.UpdateUserAsync(It.IsAny<Services.User.Core.User.User>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Can not update user info", result.Message);
            Assert.Contains("Database error", result.Message);
            
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Delete user", result.Data);
            
            _userRepositoryMock.Verify(x => x.DeleteUserAsync(existingUser), Times.Once);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Once);
            _encryptInfoMock.Verify(
                x => x.EncryptRsaObjectStrings(It.IsAny<DeleteUserEmail>(), PublicKey), 
                Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNonExistingUser_ReturnsFail()
        {
            // Arrange
            SetupUserNotFound(UserId);

            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not found user", result.Message);
            
            _userRepositoryMock.Verify(x => x.DeleteUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNullPublicKey_ReturnsFail()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns((string)null);

            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not found user", result.Message);
            
            _userRepositoryMock.Verify(x => x.DeleteUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNullUserProperties_ReturnsFail()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            existingUser.UserName = null;
            SetupExistingUser(UserId, existingUser);

            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User can not have null properties", result.Message);
            
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_WithRepositoryException_ReturnsFail()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            var exception = new Exception("Database error");
            
            SetupExistingUser(UserId, existingUser);
            
            _userRepositoryMock
                .Setup(x => x.DeleteUserAsync(It.IsAny<Services.User.Core.User.User>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Can not delete user", result.Message);
            Assert.Contains("Database error", result.Message);
            
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        #endregion

        #region Helper Methods

        private UserDto CreateValidUserDto()
        {
            return new UserDto(
                firstName: "John",
                lastName: "Doe",
                login: "john_doe@123",
                email: "john.doe@example.com",
                nickName: "johnny@123"
            );
        }

        private Services.User.Core.User.User CreateExistingUser()
        {
            var user = new Services.User.Core.User.User("john_doe@123", "johnny@123")
            {
                Id = UserId,
                Email = "john.doe@example.com",
                UserName = "John Doe"
            };
            return user;
        }

        private void SetupValidationSuccess(UserDto userDto)
        {
            _validatorMock
                .Setup(x => x.ValidateAsync(userDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private void SetupValidationFailure(UserDto userDto, List<ValidationFailure> errors)
        {
            var validationResult = new ValidationResult(errors);
            _validatorMock
                .Setup(x => x.ValidateAsync(userDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);
        }

        private void SetupExistingUser(string userId, Services.User.Core.User.User user)
        {
            _userRepositoryMock
                .Setup(x => x.FindUserByIdAsync(userId))
                .ReturnsAsync(user);
        }

        private void SetupUserNotFound(string userId)
        {
            _userRepositoryMock
                .Setup(x => x.FindUserByIdAsync(userId))
                .ReturnsAsync((Services.User.Core.User.User)null!);
        }

        private void SetupMappingBehavior(UserDto source, Services.User.Core.User.User destination)
        {
            _mapperMock
                .Setup(x => x.Map(source, destination));
        }

        #endregion
    }
