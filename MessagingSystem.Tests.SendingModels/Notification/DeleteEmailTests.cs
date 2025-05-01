using MessagingSystem.SendingModels.UserNotification;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.Notification
{
    public class DeleteUserEmailTests
    {
        [Fact]
        public void Should_Create_DeleteUserEmail_With_Valid_Values()
        {
            // Arrange
            const string email = "test@example.com";
            const string userName = "testUser";

            // Act
            var deleteUserEmail = new DeleteUserEmail(email, userName);

            // Assert
            Assert.Equal(email, deleteUserEmail.Email);
            Assert.Equal(userName, deleteUserEmail.UserName);
        }

        [Fact]
        public void Should_Set_Email_Through_Setter()
        {
            // Arrange
            var deleteUserEmail = new DeleteUserEmail("initial@example.com", "testUser");

            // Act
            const string newEmail = "newEmail@example.com";
            deleteUserEmail.Email = newEmail;

            // Assert
            Assert.Equal(newEmail, deleteUserEmail.Email);
        }

        [Fact]
        public void Should_Set_UserName_Through_Setter()
        {
            // Arrange
            var deleteUserEmail = new DeleteUserEmail("test@example.com", "initialUser");

            // Act
            const string newUserName = "newUser";
            deleteUserEmail.UserName = newUserName;

            // Assert
            Assert.Equal(newUserName, deleteUserEmail.UserName);
        }

        [Fact]
        public void Should_Set_Email_And_UserName_Through_Setters()
        {
            // Arrange
            var deleteUserEmail = new DeleteUserEmail("test@example.com", "testUser");

            // Act
            const string newEmail = "newEmail@example.com";
            const string newUserName = "newUser";
            deleteUserEmail.Email = newEmail;
            deleteUserEmail.UserName = newUserName;

            // Assert
            Assert.Equal(newEmail, deleteUserEmail.Email);
            Assert.Equal(newUserName, deleteUserEmail.UserName);
        }
    }
}