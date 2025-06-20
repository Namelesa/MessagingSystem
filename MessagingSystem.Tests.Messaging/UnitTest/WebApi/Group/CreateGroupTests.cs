using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Group
{
    public class CreateGroupTests
    {
        [Fact]
        public void Users_Setter_ReplacesUserList()
        {
            // Arrange
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", "Admin");
            var newUsers = new List<string> { "User1", "User2" };

            // Act
            createGroup.Users = newUsers;

            // Assert
            Assert.Equal(2, createGroup.Users.Count);
            Assert.DoesNotContain("Admin", createGroup.Users); 
            Assert.Contains("User1", createGroup.Users);
            Assert.Contains("User2", createGroup.Users);
        }
        
        [Fact]
        public void Constructor_ValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var groupName = "Test Group";
            var image = "image.jpg";
            var description = "Test Description";
            var admin = "TestAdmin";

            // Act
            var createGroup = new CreateGroup(groupName, image, description, admin);

            // Assert
            Assert.Equal(groupName, createGroup.GroupName);
            Assert.Equal(image, createGroup.Image);
            Assert.Equal(description, createGroup.Description);
            Assert.Equal(admin, createGroup.Admin);
            Assert.Single(createGroup.Users);
            Assert.Contains(admin, createGroup.Users);
        }

        [Fact]
        public void Constructor_NullImage_SetsImageAsNull()
        {
            // Arrange & Act
            var createGroup = new CreateGroup("Test Group", null, "Description", "Admin");

            // Assert
            Assert.Null(createGroup.Image);
        }

        [Xunit.Theory]
        [InlineData("")]
        [InlineData(null)]
        public void GroupName_RequiredValidation_FailsForEmptyOrNull(string groupName)
        {
            // Arrange
            var createGroup = new CreateGroup("Valid", "image.jpg", "Description", "Admin")
            {
                GroupName = groupName
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.GroupName)) && 
                v.ErrorMessage == "Group name is required");
        }

        [Xunit.Theory]
        [InlineData(351)] 
        [InlineData(500)] 
        public void GroupName_StringLengthValidation_FailsForTooLong(int length)
        {
            // Arrange
            var longGroupName = new string('A', length);
            var createGroup = new CreateGroup("Valid", "image.jpg", "Description", "Admin")
            {
                GroupName = longGroupName
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.GroupName)) && 
                v.ErrorMessage == "Group name length must be between 1 and 350 characters.");
        }
        
        [Xunit.Theory]
        [InlineData("")]
        public void Description_StringLengthValidation_FailsForEmptyOrNull(string description)
        {
            // Arrange
            var createGroup = new CreateGroup("Group", "image.jpg", "Valid Description", "Admin")
            {
                Description = description
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.Description)) && 
                v.ErrorMessage == "Group description length must be between 1 and 650 characters.");
        }

        [Xunit.Theory]
        [InlineData(651)] 
        [InlineData(1000)]
        public void Description_StringLengthValidation_FailsForTooLong(int length)
        {
            // Arrange
            var longDescription = new string('A', length);
            var createGroup = new CreateGroup("Group", "image.jpg", "Valid Description", "Admin")
            {
                Description = longDescription
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.Description)) && 
                v.ErrorMessage == "Group description length must be between 1 and 650 characters.");
        }
        
        [Xunit.Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Admin_RequiredValidation_FailsForEmptyOrNull(string admin)
        {
            // Arrange
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", "ValidAdmin")
            {
                Admin = admin
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.Admin)) && 
                v.ErrorMessage == "Group admin is required");
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Admin_StringLengthValidation_FailsForTooShort(int length)
        {
            // Arrange
            var shortAdmin = new string('A', length);
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", "ValidAdmin")
            {
                Admin = shortAdmin
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.Admin)) && 
                v.ErrorMessage == "Admin name length must be between 1 and 80 characters.");
        }

        [Xunit.Theory]
        [InlineData(81)] 
        [InlineData(100)]
        public void Admin_StringLengthValidation_FailsForTooLong(int length)
        {
            // Arrange
            var longAdmin = new string('A', length);
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", "ValidAdmin")
            {
                Admin = longAdmin
            };

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Contains(validationResults, v => 
                v.MemberNames.Contains(nameof(CreateGroup.Admin)) && 
                v.ErrorMessage == "Admin name length must be between 1 and 80 characters.");
        }
        
        [Fact]
        public void Users_InitializedAsEmptyList()
        {
            // Arrange & Act
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", "Admin");

            // Assert
            Assert.NotNull(createGroup.Users);
            Assert.Single(createGroup.Users); 
        }

        [Fact]
        public void Constructor_AdminAddedToUsers_Success()
        {
            // Arrange
            var admin = "TestAdmin";

            // Act
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", admin);

            // Assert
            Assert.Contains(admin, createGroup.Users);
            Assert.Single(createGroup.Users);
        }

        [Fact]
        public void ValidModel_AllValidationsPass()
        {
            // Arrange
            var createGroup = new CreateGroup(
                "Valid Group Name",
                "image.jpg",
                "Valid description for the group",
                "ValidAdmin");

            // Act
            var validationResults = ValidateModel(createGroup);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Users_CanAddAdditionalUsers()
        {
            // Arrange
            var createGroup = new CreateGroup("Group", "image.jpg", "Description", "Admin");
            
            // Act
            createGroup.Users.Add("User1");
            createGroup.Users.Add("User2");

            // Assert
            Assert.Equal(3, createGroup.Users.Count);
            Assert.Contains("Admin", createGroup.Users);
            Assert.Contains("User1", createGroup.Users);
            Assert.Contains("User2", createGroup.Users);
        }
        
        private static List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }
    }
}