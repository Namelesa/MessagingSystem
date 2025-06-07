using Microsoft.AspNetCore.Identity;
using UserModel = MessagingSystem.Services.User.Core.User.User;

namespace MessagingSystem.Tests.User.UnitTests.Core
{
    public class UserTests
    {
        [Fact]
        public void Constructor_ShouldSetLoginAndNickName()
        {
            // Arrange
            const string login = "testLogin";
            const string nickName = "testNickName";
            const string image = "testImage";

            // Act
            var user = new UserModel(login, nickName, image);

            // Assert
            Assert.Equal(login, user.Login);
            Assert.Equal(nickName, user.NickName);
        }

        [Fact]
        public void Constructor_ShouldInheritFromIdentityUser()
        {
            // Arrange & Act
            var user = new UserModel("testLogin", "testNickName", "testImage");

            // Assert
            Assert.IsAssignableFrom<IdentityUser>(user);
        }

        [Fact]
        public void HashProperties_ShouldBeNullByDefault()
        {
            // Arrange & Act
            var user = new UserModel("testLogin", "testNickName", "testImage");

            // Assert
            Assert.Null(user.HashLogin);
            Assert.Null(user.HashEmail);
            Assert.Null(user.HashNickName);
        }

        [Fact]
        public void SetHashes_ShouldSetAllHashProperties()
        {
            // Arrange
            var user = new UserModel("testLogin", "testNickName", "testImage");
            const string loginHash = "loginHashValue";
            const string emailHash = "emailHashValue";
            const string nickNameHash = "nickNameHashValue";

            // Act
            user.SetHashes(loginHash, emailHash, nickNameHash);

            // Assert
            Assert.Equal(loginHash, user.HashLogin);
            Assert.Equal(emailHash, user.HashEmail);
            Assert.Equal(nickNameHash, user.HashNickName);
        }

        [Fact]
        public void Login_PropertyGetter_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedLogin = "userLogin123";
            var user = new UserModel(expectedLogin, "someNickname", "testImage");

            // Act
            var actualLogin = user.Login;

            // Assert
            Assert.Equal(expectedLogin, actualLogin);
        }

        [Fact]
        public void NickName_PropertyGetter_ShouldReturnCorrectValue()
        {
            // Arrange
            const string expectedNickName = "coolUser42";
            var user = new UserModel("someLogin", expectedNickName, "testImage");

            // Act
            var actualNickName = user.NickName;

            // Assert
            Assert.Equal(expectedNickName, actualNickName);
        }

        [Fact]
        public void Login_PropertySetter_ShouldSetValue()
        {
            // Arrange
            const string initialLogin = "initialLogin";
            const string newLogin = "newLogin";
            var user = new UserModel(initialLogin, "someNickname", "testImage");
            
            // Act
            typeof(UserModel).GetProperty("Login")
                ?.SetValue(user, newLogin, null);
            
            // Assert
            Assert.Equal(newLogin, user.Login);
        }

        [Fact]
        public void NickName_PropertySetter_ShouldSetValue()
        {
            // Arrange
            const string initialNickName = "initialNickName";
            const string newNickName = "newNickName";
            var user = new UserModel("someLogin", initialNickName, "testIamge");
            
            // Act
            typeof(UserModel).GetProperty("NickName")
                ?.SetValue(user, newNickName, null);
            
            // Assert
            Assert.Equal(newNickName, user.NickName);
        }
    }
}