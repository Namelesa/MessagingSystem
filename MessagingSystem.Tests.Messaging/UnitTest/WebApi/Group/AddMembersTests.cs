using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.Members;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Group
{
    public class AddMembersTests
    {
        [Fact]
        public void Constructor_InitializesEmptyUsersList()
        {
            // Arrange & Act
            var model = new AddMembers();

            // Assert
            Assert.NotNull(model.Users);
            Assert.Empty(model.Users);
        }

        [Fact]
        public void Set_UsersProperty_SetsCorrectly()
        {
            // Arrange
            var model = new AddMembers();
            var newUsers = new List<string> { "User1", "User2" };

            // Act
            model.Users = newUsers;

            // Assert
            Assert.Equal(2, model.Users.Count);
            Assert.Contains("User1", model.Users);
            Assert.Contains("User2", model.Users);
        }

        [Fact]
        public void Users_CanAddMembersToList()
        {
            // Arrange
            var model = new AddMembers();

            // Act
            model.Users.Add("UserA");
            model.Users.Add("UserB");

            // Assert
            Assert.Equal(2, model.Users.Count);
            Assert.Contains("UserA", model.Users);
            Assert.Contains("UserB", model.Users);
        }

        [Fact]
        public void Users_SetToNull_AllowsNullAssignment()
        {
            // Arrange
            var model = new AddMembers
            {
                // Act
                Users = null!
            };

            // Assert
            Assert.Null(model.Users);
        }
    }
}