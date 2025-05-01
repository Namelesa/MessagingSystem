using MessagingSystem.SendingModels.UserNotification;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.Notification
{
    public class EditUserEmailTests
    {
        [Fact]
        public void Should_Create_EditUserEmail_With_Valid_Values()
        {
            // Arrange
            const string email = "test@example.com";
            const string userName = "testUser";

            // Act
            var editUserEmail = new EditUserEmail(email, userName);

            // Assert
            Assert.Equal(email, editUserEmail.Email);
            Assert.Equal(userName, editUserEmail.UserName);
        }

        [Fact]
        public void Should_Set_Email_Through_Setter()
        {
            // Arrange
            var editUserEmail = new EditUserEmail("initial@example.com", "testUser");

            // Act
            const string newEmail = "newEmail@example.com";
            editUserEmail.Email = newEmail;

            // Assert
            Assert.Equal(newEmail, editUserEmail.Email);
        }

        [Fact]
        public void Should_Set_UserName_Through_Setter()
        {
            // Arrange
            var editUserEmail = new EditUserEmail("test@example.com", "initialUser");

            // Act
            const string newUserName = "newUser";
            editUserEmail.UserName = newUserName;

            // Assert
            Assert.Equal(newUserName, editUserEmail.UserName);
        }

        [Fact]
        public void Should_Set_Email_And_UserName_Through_Setters()
        {
            // Arrange
            var editUserEmail = new EditUserEmail("test@example.com", "testUser");

            // Act
            const string newEmail = "newEmail@example.com";
            const string newUserName = "newUser";
            editUserEmail.Email = newEmail;
            editUserEmail.UserName = newUserName;

            // Assert
            Assert.Equal(newEmail, editUserEmail.Email);
            Assert.Equal(newUserName, editUserEmail.UserName);
        }
    }
}