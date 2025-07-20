using System.Security.Claims;
using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.User.Application.User;
using MessagingSystem.Services.User.Application.User.Dto;
using MessagingSystem.Services.User.Core.User;
using MessagingSystem.Services.User.Infrastructure.HasherInfo;
using MessagingSystem.Services.User.Infrastructure.ImageLoader;
using MessagingSystem.Services.User.Infrastructure.Jwt;
using MessagingSystem.Services.User.Infrastructure.Keys;
using Microsoft.AspNetCore.Http;
using Moq;
using UserEntity = MessagingSystem.Services.User.Core.User.User;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace MessagingSystem.Tests.User.UnitTests.Application.User
{
    public class UserOrchestratorTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<UserDto>> _mockValidator;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEncryptionInfo> _mockEncryptionInfo;
        private readonly Mock<IDecryptionInfo> _mockDecryptionInfo;
        private readonly Mock<IHasher> _mockHasher;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IRequestClient<EditUserInfoRequest>> _mockEditClient;
        private readonly Mock<IRequestClient<DeleteUserInfoRequest>> _mockDeleteClient;
        private readonly Mock<IPublicKeyStorage> _mockPublicKeyStorage;
        private readonly Mock<IImageLoaderService> _mockImageLoaderService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly UserOrchestrator _userOrchestrator;
        private readonly Mock<IJwtService> _mockJwtService;

        public UserOrchestratorTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<UserDto>>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEncryptionInfo = new Mock<IEncryptionInfo>();
            _mockDecryptionInfo = new Mock<IDecryptionInfo>();
            _mockHasher = new Mock<IHasher>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockEditClient = new Mock<IRequestClient<EditUserInfoRequest>>();
            _mockDeleteClient = new Mock<IRequestClient<DeleteUserInfoRequest>>();
            _mockPublicKeyStorage = new Mock<IPublicKeyStorage>();
            _mockImageLoaderService = new Mock<IImageLoaderService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockJwtService = new Mock<IJwtService>();

            _userOrchestrator = new UserOrchestrator(
                _mockMapper.Object,
                _mockValidator.Object,
                _mockUserRepository.Object,
                _mockEncryptionInfo.Object,
                _mockDecryptionInfo.Object,
                _mockHasher.Object,
                _mockPublishEndpoint.Object,
                _mockEditClient.Object,
                _mockDeleteClient.Object,
                _mockPublicKeyStorage.Object,
                _mockImageLoaderService.Object,
                _mockHttpContextAccessor.Object,
                _mockJwtService.Object);
        }

        private UserEntity CreateExistingUser()
        {
            var user = new UserEntity("TestLogin", "testNick", "encrypted_image_url")
            {
                UserName = "TestUser",
                Email = "test@example.com",
            };
            user.SetHashes("hashed_login", "hashed_email", "hashed_nick_name");
            return user;
        }
        
        private UserEntity CreateExistingUserWithOutImage()
        {
            var user = new UserEntity("TestLogin", "testNick", "")
            {
                UserName = "TestUser",
                Email = "test@example.com",
            };
            user.SetHashes("hashed_login", "hashed_email", "hashed_nick_name");
            return user;
        }

        private void SetupHttpContext(string nickName)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.UserData, nickName) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        }

        #region GetUserInfoAsync Tests

        [Fact]
        public async Task GetUserInfoAsync_AccessDenied_WhenNickNamesDontMatch()
        {
            // Arrange
            var requestedNickName = "testnick";
            var currentNickName = "othernick";
            SetupHttpContext(currentNickName);

            // Act
            var result = await _userOrchestrator.GetUserInfoAsync(requestedNickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Access denied", result.Message);
        }

        [Fact]
        public async Task GetUserInfoAsync_UserNotFound_WhenUserDoesntExist()
        {
            // Arrange
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            SetupHttpContext(nickName);
            
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync((UserEntity)null);

            // Act
            var result = await _userOrchestrator.GetUserInfoAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
        }

        [Fact]
        public async Task GetUserInfoAsync_Success_WhenUserExistsAndAccessGranted()
        {
            // Arrange
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            var existingUser = CreateExistingUser();
            var userDto = new UserDto("firstName", "lastName", "testLogin", "testEmail", nickName, "");
            
            SetupHttpContext(nickName);
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);
            _mockMapper.Setup(x => x.Map<UserDto>(existingUser)).Returns(userDto);

            // Act
            var result = await _userOrchestrator.GetUserInfoAsync(nickName);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(userDto, result.Data);
            _mockDecryptionInfo.Verify(x => x.DecryptObjectStrings(existingUser), Times.Once);
        }

        #endregion

        #region EditUserInfoAsync Tests
        
        [Fact]
        public async Task EditUserInfoAsync_UserNotFound_WhenUserDoesntExist()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "testLogin", "testEmail", "testNick", "");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(() => new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync((UserEntity)null);

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
        }

        [Fact]
        public async Task EditUserInfoAsync_Fail_WhenUserHasNullProperties()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email@test.com", "newNick", "");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";

            var existingUser = CreateExistingUser();
            existingUser.UserName = null; 

            SetupHttpContext(nickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User can not have null properties", result.Message);
        }
        
        [Fact]
        public async Task EditUserInfoAsync_AccessDenied_WhenNickNamesDontMatch()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "testLogin", "testEmail", "newNick", "");
            var nickName = "testnick";
            var currentNickName = "othernick";
            var hashedNickName = "hashed_testnick";
            var existingUser = CreateExistingUser();
            
            SetupHttpContext(currentNickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(() => new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Access denied", result.Message);
        }

        [Fact]
        public async Task EditUserInfoAsync_Success_WhenAllConditionsMet()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email@test.com", "newNick", "");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            var existingUser = CreateExistingUser();
            var editResponse = new Mock<Response<EditUserRollBack>>();
            var editRollBack = new EditUserRollBack(true);
            
            SetupHttpContext(nickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(()=> new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockHasher.Setup(x => x.Hash(userDto.Login)).Returns("hashed_login");
            _mockHasher.Setup(x => x.Hash(userDto.Email)).Returns("hashed_email");
            _mockHasher.Setup(x => x.Hash(userDto.NickName)).Returns("hashed_newnick");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);
            _mockDecryptionInfo.Setup(x => x.Decrypt(existingUser.Image)).Returns("http://decrypted_image.jpg");
            editResponse.Setup(x => x.Message).Returns(editRollBack);
            _mockEditClient.Setup(x => x.GetResponse<EditUserRollBack>(It.IsAny<EditUserInfoRequest>(), default, default))
                .ReturnsAsync(editResponse.Object);

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Update user info", result.Data);
            _mockUserRepository.Verify(x => x.UpdateUserAsync(existingUser), Times.Once);
            _mockPublishEndpoint.Verify(x => x.Publish(It.IsAny<EditUserEmail>(), default), Times.Once);
        }

        [Fact]
        public async Task EditUserInfoAsync_Fail_WhenEditResponseNotSuccessful()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email@test.com", "newNick", "");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            var existingUser = CreateExistingUser();
            var editResponse = new Mock<Response<EditUserRollBack>>();
            var editRollBack = new EditUserRollBack(false);
            
            SetupHttpContext(nickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(()=> new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockHasher.Setup(x => x.Hash(userDto.Login)).Returns("hashed_login");
            _mockHasher.Setup(x => x.Hash(userDto.Email)).Returns("hashed_email");
            _mockHasher.Setup(x => x.Hash(userDto.NickName)).Returns("hashed_newnick");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);
            editResponse.Setup(x => x.Message).Returns(editRollBack);
            _mockEditClient.Setup(x => x.GetResponse<EditUserRollBack>(It.IsAny<EditUserInfoRequest>(), default, default))
                .ReturnsAsync(editResponse.Object);

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Can't update user info", result.Message);
        }
        
        [Fact]
        public async Task EditUserInfoAsync_Fail_WhenExceptionThrownInsideTry()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email@test.com", "newNick", "");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";

            var existingUser = CreateExistingUser();

            SetupHttpContext(nickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockHasher.Setup(x => x.Hash(userDto.Login)).Returns("hashed_login");
            _mockHasher.Setup(x => x.Hash(userDto.Email)).Returns("hashed_email");
            _mockHasher.Setup(x => x.Hash(userDto.NickName)).Returns("hashed_newnick");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);
            
            _mockEditClient.Setup(x => x.GetResponse<EditUserRollBack>(It.IsAny<EditUserInfoRequest>(), default, default))
                .ThrowsAsync(new Exception("Forced failure"));

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Can not update user info", result.Message);
            Assert.Contains("Forced failure", result.Message);
        }

        [Fact]
        public async Task EditUserInfoAsync_Fail_WhenValidationFails()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email", "nick", "");
            var nickName = "testnick";

            var failures = new List<FluentValidation.Results.ValidationFailure>
            {
                new("Login", "Login is required"),
                new("Email", "Email is invalid")
            };
            var failedResult = new ValidationResult(failures);

            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(failedResult);

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Login is required", result.Message);
            Assert.Contains("Email is invalid", result.Message);
        }
        
        [Fact]
        public async Task EditUserInfoAsync_Fail_WhenExceptionThrown()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email@test.com", "newNick", "");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            var existingUser = CreateExistingUser();
            
            SetupHttpContext(nickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(()=> new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);
            _mockEditClient.Setup(x => x.GetResponse<EditUserRollBack>(It.IsAny<EditUserInfoRequest>(), default, default))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User can not have null properties", result.Message);
        }
        
        [Fact]
        public async Task EditUserInfoAsync_DeletesOldImage_WhenNewImageIsProvided()
        {
            // Arrange
            var userDto = new UserDto("firstName", "lastName", "login", "email@test.com", "newNick", "newImage.jpg");
            var nickName = "testnick";
            var hashedNickName = "hashed_testnick";
            var existingUser = CreateExistingUser();

            SetupHttpContext(nickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockValidator.Setup(x => x.ValidateAsync(userDto, default)).ReturnsAsync(new ValidationResult());
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(hashedNickName);
            _mockHasher.Setup(x => x.Hash(userDto.Login)).Returns("hashed_login");
            _mockHasher.Setup(x => x.Hash(userDto.Email)).Returns("hashed_email");
            _mockHasher.Setup(x => x.Hash(userDto.NickName)).Returns("hashed_newnick");

            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(hashedNickName))
                .ReturnsAsync(existingUser);

            // image decryption returns a full URL
            _mockDecryptionInfo.Setup(x => x.Decrypt(existingUser.Image)).Returns("https://cdn.example.com/images/test-key.jpg");
            _mockEditClient.Setup(x => x.GetResponse<EditUserRollBack>(It.IsAny<EditUserInfoRequest>(), default, default))
                .ReturnsAsync(new Mock<Response<EditUserRollBack>> { DefaultValue = DefaultValue.Mock }.Object);

            var editResponse = new EditUserRollBack(true);
            _mockEditClient.Setup(x => x.GetResponse<EditUserRollBack>(It.IsAny<EditUserInfoRequest>(), default, default))
                .ReturnsAsync(Mock.Of<Response<EditUserRollBack>>(r => r.Message == editResponse));

            // Act
            var result = await _userOrchestrator.EditUserInfoAsync(userDto, nickName);

            // Assert
            Assert.True(result.Success);
            _mockImageLoaderService.Verify(x => x.DeleteAsync("test-key.jpg"), Times.Once);
        }
        
        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_WhenUserDoesntExist()
        {
            // Arrange
            var nickName = "testnick";
            
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(nickName))
                .ReturnsAsync((UserEntity)null);

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Not found user", result.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_AccessDenied_WhenNickNamesDontMatch()
        {
            // Arrange
            var nickName = "testnick";
            var currentNickName = "othernick";
            var existingUser = CreateExistingUser();
            
            SetupHttpContext(currentNickName);
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(existingUser.HashNickName); 
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(existingUser.HashNickName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Access denied", result.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_Fail_WhenUserHasNullProperties()
        {
            // Arrange
            var nickName = "testnick";
            var existingUser = CreateExistingUser();
            existingUser.Email = null; 

            SetupHttpContext(nickName);
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(existingUser.HashNickName); 
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(existingUser.HashNickName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User can not have null properties", result.Message);
        }

        
        [Fact]
        public async Task DeleteUserAsync_Success_WhenAllConditionsMet()
        {
            // Arrange
            var nickName = "testnick";
            var existingUser = CreateExistingUser();
            var deleteResponse = new Mock<Response<DeleteUserInfoRollback>>();
            var deleteRollback = new DeleteUserInfoRollback(existingUser.HashNickName)
            {
                IsSuccess = true
            };
    
            SetupHttpContext(nickName);
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(existingUser.HashNickName); 
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(existingUser.HashNickName))
                .ReturnsAsync(existingUser);
            _mockDecryptionInfo.Setup(x => x.Decrypt(existingUser.Image)).Returns("http://decrypted_image.jpg");
            deleteResponse.Setup(x => x.Message).Returns(deleteRollback);
            _mockDeleteClient.Setup(x => x.GetResponse<DeleteUserInfoRollback>(It.IsAny<DeleteUserInfoRequest>(), default, default))
                .ReturnsAsync(deleteResponse.Object);

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Delete user", result.Data);
            
            _mockUserRepository.Verify(x => x.DeleteUserAsync(existingUser), Times.Once);
            _mockPublishEndpoint.Verify(x => x.Publish(It.IsAny<DeleteUserEmail>(), default), Times.Once);
            _mockImageLoaderService.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_Fail_WhenDeleteResponseNotSuccessful()
        {
            // Arrange
            var nickName = "testnick";
            var existingUser = CreateExistingUser();
            var deleteResponse = new Mock<Response<DeleteUserInfoRollback>>();
            var deleteRollback = new DeleteUserInfoRollback(existingUser.HashNickName)
            {
                IsSuccess = false
            };

            SetupHttpContext(nickName);
    
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(existingUser.HashNickName); 
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(existingUser.HashNickName))
                .ReturnsAsync(existingUser);
            deleteResponse.Setup(x => x.Message).Returns(deleteRollback);
            _mockDeleteClient.Setup(x => x.GetResponse<DeleteUserInfoRollback>(It.IsAny<DeleteUserInfoRequest>(), default, default))
                .ReturnsAsync(deleteResponse.Object);

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Can't delete user info", result.Message);
        }

        [Fact]
        public async Task DeleteUserAsync_Fail_WhenExceptionThrown()
        {
            // Arrange
            var nickName = "testnick";
            var existingUser = CreateExistingUser();
            
            SetupHttpContext(nickName);
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(existingUser.HashNickName); 
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(existingUser.HashNickName))
                .ReturnsAsync(existingUser);
            _mockDeleteClient.Setup(x => x.GetResponse<DeleteUserInfoRollback>(It.IsAny<DeleteUserInfoRequest>(), default, default))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Can not delete user", result.Message);
            Assert.Contains("Test exception", result.Message);
        }
        
        [Fact]
        public async Task DeleteUserAsync_Success_WhenImageIsNull()
        {
            // Arrange
            var nickName = "testnick";
            var existingUser = CreateExistingUserWithOutImage();

            var deleteResponse = new Mock<Response<DeleteUserInfoRollback>>();
            var deleteRollback = new DeleteUserInfoRollback(existingUser.HashNickName)
            {
                IsSuccess = true
            };

            SetupHttpContext(nickName);
            _mockHasher.Setup(x => x.Hash(nickName)).Returns(existingUser.HashNickName);
            _mockPublicKeyStorage.Setup(x => x.Get("Notification")).Returns("notification_key");
            _mockPublicKeyStorage.Setup(x => x.Get("Messaging")).Returns("messaging_key");
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(existingUser.HashNickName))
                .ReturnsAsync(existingUser);
            deleteResponse.Setup(x => x.Message).Returns(deleteRollback);
            _mockDeleteClient.Setup(x => x.GetResponse<DeleteUserInfoRollback>(It.IsAny<DeleteUserInfoRequest>(), default, default))
                .ReturnsAsync(deleteResponse.Object);

            // Act
            var result = await _userOrchestrator.DeleteUserAsync(nickName);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Delete user", result.Data);

            _mockUserRepository.Verify(x => x.DeleteUserAsync(existingUser), Times.Once);
            _mockPublishEndpoint.Verify(x => x.Publish(It.IsAny<DeleteUserEmail>(), default), Times.Once);
            _mockImageLoaderService.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never); // ✅ Не должен вызываться
        }
        
        #endregion

        #region FindUserByNickNameAsync Tests

        [Fact]
        public async Task FindUserByNickNameAsync_UserNotFound_WhenUserDoesntExist()
        {
            // Arrange
            var nickName = "testnick";
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(nickName))
                .ReturnsAsync((UserEntity)null);

            // Act
            var result = await _userOrchestrator.FindUserByNickNameAsync(nickName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found", result.Message);
        }

        [Fact]
        public async Task FindUserByNickNameAsync_Success_WhenUserExists()
        {
            // Arrange
            var nickName = "testnick";
            var existingUser = CreateExistingUser();
            _mockUserRepository.Setup(x => x.FindUserByHashNickNameAsync(nickName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _userOrchestrator.FindUserByNickNameAsync(nickName);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(existingUser.NickName, result.Data?.UserNickName);
            Assert.Equal(existingUser.Image, result.Data?.Image);
        }

        #endregion

        #region FindUsersByNickNamesAsync Tests

        [Fact]
        public async Task FindUsersByNickNamesAsync_UsersNotFound_WhenUsersDoesntExist()
        {
            // Arrange
            var nickNames = new List<string> { "nick1", "nick2" };
            _mockUserRepository.Setup(x => x.FindUsersByHashNickNamesAsync(nickNames))
                .ReturnsAsync((List<UserEntity>)null);

            // Act
            var result = await _userOrchestrator.FindUsersByNickNamesAsync(nickNames);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Users not found", result.Message);
        }

        [Fact]
        public async Task FindUsersByNickNamesAsync_Success_WhenUsersExist()
        {
            // Arrange
            var nickNames = new List<string> { "nick1", "nick2" };
            var users = new List<UserEntity> 
            { 
                CreateExistingUser(),
                CreateExistingUser() 
            };
            _mockUserRepository.Setup(x => x.FindUsersByHashNickNamesAsync(nickNames))
                .ReturnsAsync(users);

            // Act
            var result = await _userOrchestrator.FindUsersByNickNamesAsync(nickNames);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, result.Data?.Count);
        }

        #endregion
    }
}