using MessagingSystem.SendingModels.UserNotification;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.Notification
{
    public class ConfirmUserEmailTests
    {
        [Fact]
        public void Should_Create_ConfirmUserEmail_With_Valid_Values()
        {
            // Arrange
            const string userName = "testUser";
            const string email = "test@example.com";
            const string nickName = "testNick";

            // Act
            var confirmUserEmail = new ConfirmUserEmail(userName, email, nickName);

            // Assert
            Assert.Equal(userName, confirmUserEmail.UserName);
            Assert.Equal(email, confirmUserEmail.Email);
            Assert.Equal(nickName, confirmUserEmail.NickName);
        }

        [Fact]
        public void Should_Set_UserName_Through_Setter()
        {
            // Arrange
            var confirmUserEmail = new ConfirmUserEmail("initialUser", "test@example.com", "testNick");

            // Act
            const string newUserName = "newUser";
            confirmUserEmail.UserName = newUserName;

            // Assert
            Assert.Equal(newUserName, confirmUserEmail.UserName);
        }

        [Fact]
        public void Should_Set_NickName_Through_Setter()
        {
            // Arrange
            var confirmUserEmail = new ConfirmUserEmail("initialUser", "test@example.com", "initialNick");

            // Act
            const string newNickName = "newNick";
            confirmUserEmail.NickName = newNickName;

            // Assert
            Assert.Equal(newNickName, confirmUserEmail.NickName);
        }

        [Fact]
        public void Should_Set_Email_Through_Setter()
        {
            // Arrange
            var confirmUserEmail = new ConfirmUserEmail("initialUser", "test@example.com", "testNick");

            // Act
            const string newEmail = "newEmail@example.com";
            confirmUserEmail.Email = newEmail;

            // Assert
            Assert.Equal(newEmail, confirmUserEmail.Email);
        }
        
        [Fact]
        public void Should_Set_Email_And_NickName_Through_Setters()
        {
            // Arrange
            var confirmUserEmail = new ConfirmUserEmail("initialUser", "test@example.com", "testNick");

            // Act
            const string newEmail = "newEmail@example.com";
            const string newNickName = "newNick";
            confirmUserEmail.Email = newEmail;
            confirmUserEmail.NickName = newNickName;

            // Assert
            Assert.Equal(newEmail, confirmUserEmail.Email);
            Assert.Equal(newNickName, confirmUserEmail.NickName);
        }
    }
}
