using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using FluentValidation.Results;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
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
        private readonly Mock<IHasher> _hasherMock;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<IPublicKeyStorage> _publicKeyStorageMock;
        private readonly UserOrchestrator _orchestrator;
        private readonly Mock<IRequestClient<EditUserInfoRequest>> _clientEdit;
        private readonly Mock<IRequestClient<DeleteUserInfoRequest>> _clientDelete;

        private const string UserId = "user123";
        private const string PublicKey = "test-public-key";
        private const string HashedValue = "hashed-value";

        public UserOrchestratorTests()
        {
            _mapperMock = new Mock<IMapper>();
            _validatorMock = new Mock<IValidator<UserDto>>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _encryptInfoMock = new Mock<IEncryptionInfo>();
            Mock<IDecryptionInfo> decryptInfoMock = new();
            _hasherMock = new Mock<IHasher>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _publicKeyStorageMock = new Mock<IPublicKeyStorage>();
            _clientEdit = new Mock<IRequestClient<EditUserInfoRequest>>();
            _clientDelete = new Mock<IRequestClient<DeleteUserInfoRequest>>();
            _clientDelete = new Mock<IRequestClient<DeleteUserInfoRequest>>();
            Mock<IImageLoaderService> imageLoaderServiceMock = new();

            _orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderServiceMock.Object
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

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
            
            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);
    
            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
            
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

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
            
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

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
            
            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);
    
            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
            
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

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var deleteUserRollBack = new DeleteUserInfoRollback("pvgD3zQ83QncVIyZLKBFzadgLY/6n/NqXt8LbtvaU2U=") 
            { 
                IsSuccess = true 
            };

            var mockResponse = new Mock<Response<DeleteUserInfoRollback>>();
            mockResponse.Setup(r => r.Message).Returns(deleteUserRollBack);
            
            _clientDelete
                .Setup(x => x.GetResponse<DeleteUserInfoRollback>(
                    It.IsAny<DeleteUserInfoRequest>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
            
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

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
            
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

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
            
            var mockResponse = new Mock<Response<DeleteUserInfoRollback>>();
            var deleteUserRollBack = new DeleteUserInfoRollback("pvgD3zQ83QncVIyZLKBFzadgLY/6n/NqXt8LbtvaU2U=")
                {
                    IsSuccess = true
                };
            mockResponse.Setup(x => x.Message).Returns(deleteUserRollBack);
    
            _clientDelete.Setup(x => x.GetResponse<DeleteUserInfoRollback>(
                    It.IsAny<DeleteUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
            
            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Can not delete user", result.Message);
            
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        #endregion

        #region FindUserByNickNameAsync Tests

        [Fact]
        public async Task FindUserByNickNameAsync_WithExistingUser_ReturnsSuccess()
        {
            // Arrange
            const string nickName = "test_nickname";
            var existingUser = CreateExistingUser();
        
            _userRepositoryMock
                .Setup(x => x.FindUserByHashNickNameAsync(nickName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _orchestrator.FindUserByNickNameAsync(nickName);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(existingUser.NickName, result.Data.UserNickName);
            Assert.Equal(existingUser.Image, result.Data.Image);
        }

        [Fact]
        public async Task FindUserByNickNameAsync_WithNonExistingUser_ReturnsFail()
        {
            // Arrange
            const string nickName = "non_existing_nickname";
    
            _userRepositoryMock
                .Setup(x => x.FindUserByHashNickNameAsync(nickName))
                .ReturnsAsync((Services.User.Core.User.User)null!);

            // Act
            var result = await _orchestrator.FindUserByNickNameAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
            Assert.Null(result.Data);
        }

        #endregion

        #region FindUsersByNickNamesAsync Tests

        [Fact]
        public async Task FindUsersByNickNamesAsync_WithExistingUsers_ReturnsSuccess()
        {
            // Arrange
            var nickNames = new List<string> { "nick1", "nick2" };
            var users = new List<Services.User.Core.User.User>
            {
                CreateExistingUser(),
                CreateExistingUser()
            };
    
            _userRepositoryMock
                .Setup(x => x.FindUsersByHashNickNamesAsync(nickNames))
                .ReturnsAsync(users);

            // Act
            var result = await _orchestrator.FindUsersByNickNamesAsync(nickNames);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal(users[0].NickName, result.Data[0].UserNickName);
            Assert.Equal(users[1].NickName, result.Data[1].UserNickName);
        }

        [Fact]
        public async Task FindUsersByNickNamesAsync_WithNonExistingUsers_ReturnsFail()
        {
            // Arrange
            var nickNames = new List<string> { "non_existing1", "non_existing2" };
    
            _userRepositoryMock
                .Setup(x => x.FindUsersByHashNickNamesAsync(nickNames))
                .ReturnsAsync((List<Services.User.Core.User.User>)null!);

            // Act
            var result = await _orchestrator.FindUsersByNickNamesAsync(nickNames);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Users not found", result.Message);
            Assert.Null(result.Data);
        }

        #endregion

        #region EditUserInfoAsync Additional Tests

        [Fact]
        public async Task EditUserInfoAsync_WithRollbackFailure_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
    
            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(false); // IsSuccess = false
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
    
            // Act
            var result = await _orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Can't update user info", result.Message);
    
            _userRepositoryMock.Verify(x => x.UpdateUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<EditUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task EditUserInfoAsync_WithNullMessagingPublicKey_ReturnsFail()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();
    
            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
    
            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns((string)null);

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
        public async Task EditUserInfoAsync_WithExistingImageDeletion_ReturnsSuccess()
        {
            // Arrange
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
    
            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
    
            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock.Setup(x => x.Decrypt("encrypted_image_url"))
                .Returns("https://example.com/bucket/image.jpg");

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
        Mock.Of<IImageLoaderService>()
            );
    
            // Act
            var result = await orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Update user info", result.Data);
        }
        #endregion    

        #region DeleteUserAsync Additional Tests

        [Fact]
        public async Task DeleteUserAsync_WithRollbackFailure_ReturnsFail()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var deleteUserRollBack = new DeleteUserInfoRollback("test") 
            { 
                IsSuccess = false
            };

            var mockResponse = new Mock<Response<DeleteUserInfoRollback>>();
            mockResponse.Setup(r => r.Message).Returns(deleteUserRollBack);
    
            _clientDelete
                .Setup(x => x.GetResponse<DeleteUserInfoRollback>(
                    It.IsAny<DeleteUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(),
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
    
            // Act
            var result = await _orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Can't delete user info", result.Message);
    
            _userRepositoryMock.Verify(x => x.DeleteUserAsync(It.IsAny<Services.User.Core.User.User>()), Times.Never);
            _publishEndpointMock.Verify(
                x => x.Publish(It.IsAny<DeleteUserEmail>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNullMessagingPublicKey_ReturnsFail()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns((string)null);

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
        public async Task DeleteUserAsync_WithNullEmailProperty_ReturnsFail()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            existingUser.Email = null;
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
    
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
        public async Task DeleteUserAsync_WithNullHashNickNameProperty_ReturnsFail()
        {
            CreateExistingUser();
            var userWithoutHashes = new Services.User.Core.User.User("login", "nick", "image")
            {
                Id = UserId,
                Email = "test@example.com",
                UserName = "Test User"
            };
    
            SetupExistingUser(UserId, userWithoutHashes);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
    
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
        public async Task DeleteUserAsync_WithImageDeletion_ReturnsSuccess()
        {
            // Arrange
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var deleteUserRollBack = new DeleteUserInfoRollback("test") 
            { 
                IsSuccess = true 
            };

            var mockResponse = new Mock<Response<DeleteUserInfoRollback>>();
            mockResponse.Setup(r => r.Message).Returns(deleteUserRollBack);
    
            _clientDelete
                .Setup(x => x.GetResponse<DeleteUserInfoRollback>(
                    It.IsAny<DeleteUserInfoRequest>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
    
            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock.Setup(x => x.Decrypt("encrypted_image_url"))
                .Returns("https://example.com/bucket/image.jpg");
    
            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                Mock.Of<IImageLoaderService>()
            );
    
            // Act
            var result = await orchestrator.DeleteUserAsync(UserId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Delete user", result.Data);
        }

        #endregion

        #region DeleteImage Tests
        
        [Fact]
        public async Task DeleteImage_WithEmptyUrl_DoesNotCallImageLoaderService()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);
    
            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);
    
            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock.Setup(x => x.Decrypt("encrypted_empty_url"))
                .Returns(string.Empty);

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion
        
        #region DeleteImage Additional Tests

        [Fact]
        public async Task DeleteImage_WithNullUrl_DoesNotCallImageLoaderService()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);

            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock.Setup(x => x.Decrypt("encrypted_null_url"))
                .Returns((string)null);

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteImage_WithValidUrlWithBucket_ExtractsCorrectKey()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);

            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock
                .Setup(x => x.Decrypt("encrypted_old_url"))
                .Returns("https://cdn.example.com/storage/users/images/2024/01/image.jpg");

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync("users/images/2024/01/image.jpg"), Times.Once);
        }

        [Fact]
        public async Task DeleteImage_WithSimpleUrl_ExtractsCorrectKey()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);

            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock
                .Setup(x => x.Decrypt("encrypted_old_url"))
                .Returns("https://example.com/image.jpg");

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync("image.jpg"), Times.Once);
        }

        [Fact]
        public async Task DeleteImage_WithUrlWithoutPath_HandlesCorrectly()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var userDto = CreateValidUserDto();
            var existingUser = CreateExistingUser();

            SetupValidationSuccess(userDto);
            SetupExistingUser(UserId, existingUser);
            SetupMappingBehavior(userDto, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var mockResponse = new Mock<Response<EditUserRollBack>>();
            var editUserRollBack = new EditUserRollBack(true);
            mockResponse.Setup(x => x.Message).Returns(editUserRollBack);

            _clientEdit.Setup(x => x.GetResponse<EditUserRollBack>(
                    It.IsAny<EditUserInfoRequest>(), 
                    It.IsAny<CancellationToken>(), 
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);

            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock.Setup(x => x.Decrypt("encrypted_image_url"))
                .Returns("https://example.com/");

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.EditUserInfoAsync(userDto, UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task DeleteImage_InDeleteUserWithValidUrl_ExtractsCorrectKey()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var deleteUserRollBack = new DeleteUserInfoRollback("test") 
            { 
                IsSuccess = true 
            };

            var mockResponse = new Mock<Response<DeleteUserInfoRollback>>();
            mockResponse.Setup(r => r.Message).Returns(deleteUserRollBack);

            _clientDelete
                .Setup(x => x.GetResponse<DeleteUserInfoRollback>(
                    It.IsAny<DeleteUserInfoRequest>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);

            Mock<IDecryptionInfo> decryptInfoMock = new();
            decryptInfoMock.Setup(x => x.Decrypt(It.IsAny<string>()))
                .Returns("https://example.com/bucket/user-avatar.jpg");

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                decryptInfoMock.Object,
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.DeleteUserAsync(UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync("user-avatar.jpg"), Times.Once);
        }

        [Fact]
        public async Task DeleteImage_InDeleteUserWithNullImage_DoesNotCallImageLoader()
        {
            // Arrange
            var imageLoaderMock = new Mock<IImageLoaderService>();
            var existingUser = CreateExistingUser();
            SetupExistingUser(UserId, existingUser);

            _publicKeyStorageMock.Setup(x => x.Get("Notification")).Returns(PublicKey);
            _publicKeyStorageMock.Setup(x => x.Get("Messaging")).Returns(PublicKey);

            var deleteUserRollBack = new DeleteUserInfoRollback("test") 
            { 
                IsSuccess = true 
            };

            var mockResponse = new Mock<Response<DeleteUserInfoRollback>>();
            mockResponse.Setup(r => r.Message).Returns(deleteUserRollBack);

            _clientDelete
                .Setup(x => x.GetResponse<DeleteUserInfoRollback>(
                    It.IsAny<DeleteUserInfoRequest>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<RequestTimeout>()))
                .ReturnsAsync(mockResponse.Object);

            var orchestrator = new UserOrchestrator(
                _mapperMock.Object,
                _validatorMock.Object,
                _userRepositoryMock.Object,
                _encryptInfoMock.Object,
                Mock.Of<IDecryptionInfo>(),
                _hasherMock.Object,
                _publishEndpointMock.Object,
                _clientEdit.Object,
                _clientDelete.Object,
                _publicKeyStorageMock.Object,
                imageLoaderMock.Object
            );

            // Act
            await orchestrator.DeleteUserAsync(UserId);

            // Assert
            imageLoaderMock.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
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
                nickName: "johnny@123",
                image: "https://imagesforusers.fra1.cdn.digitaloceanspaces.com/imagesforusers/photos/9290222e-da79-4173-8f5c-cd4018779578.jpg"
            );
        }

        private Services.User.Core.User.User CreateExistingUser()
        {
            var user = new Services.User.Core.User.User("john_doe@123", "johnny@123", "encrypted_old_url")
            {
                Id = UserId,
                Email = "john.doe@example.com",
                UserName = "John Doe"
            };
            user.SetHashes("qwertyui1234567", "qwertyui1234567", "qwertyui1234567");
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
