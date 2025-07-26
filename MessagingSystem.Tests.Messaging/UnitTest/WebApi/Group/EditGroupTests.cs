using System.ComponentModel.DataAnnotations;
using MessagingSystem.Services.Messaging.WebApi.Group.Info.Contracts.GroupInfo;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.WebApi.Group
{
    public class EditGroupTests
    {
        
        [Fact]
        public void Set_ImageProperty_UpdatesValue()
        {
            // Arrange
            var editGroup = new EditGroup("Group", "old.jpg", "Description")
            {
                // Act
                Image = "new.png"
            };

            // Assert
            Assert.Equal("new.png", editGroup.Image);
        }

        [Fact]
        public void Set_DescriptionProperty_UpdatesValue()
        {
            // Arrange
            var editGroup = new EditGroup("Group", "image.jpg", "Old Description")
            {
                // Act
                Description = "New Description"
            };

            // Assert
            Assert.Equal("New Description", editGroup.Description);
        }
        
        [Fact]
        public void Constructor_ValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var groupName = "Group 1";
            var image = "image.png";
            var description = "Group description";

            // Act
            var editGroup = new EditGroup(groupName, image, description);

            // Assert
            Assert.Equal(groupName, editGroup.GroupName);
            Assert.Equal(image, editGroup.Image);
            Assert.Equal(description, editGroup.Description);
        }

        [Fact]
        public void Constructor_NullableProperties_CanBeNull()
        {
            // Arrange & Act
            var editGroup = new EditGroup("Group 1", null, null);

            // Assert
            Assert.Equal("Group 1", editGroup.GroupName);
            Assert.Null(editGroup.Image);
            Assert.Null(editGroup.Description);
        }

        [Xunit.Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GroupName_RequiredValidation_FailsForNullOrEmpty(string groupName)
        {
            // Arrange
            var editGroup = new EditGroup("Temp", "image.jpg", "desc")
            {
                GroupName = groupName
            };

            // Act
            var results = ValidateModel(editGroup);

            // Assert
            Assert.Contains(results, r =>
                r.MemberNames.Contains(nameof(EditGroup.GroupName)) &&
                r.ErrorMessage == "Group name is required");
        }

        [Xunit.Theory]
        [InlineData(351)]
        public void GroupName_LengthValidation_FailsForInvalidLength(int length)
        {
            // Arrange
            var name = new string('A', length);
            var editGroup = new EditGroup("Temp", "image.jpg", "desc")
            {
                GroupName = name
            };

            // Act
            var results = ValidateModel(editGroup);

            // Assert
            Assert.Contains(results, r =>
                r.MemberNames.Contains(nameof(EditGroup.GroupName)) &&
                r.ErrorMessage == "Group name length must be between 1 and 350 characters.");
        }
        
        
        [Xunit.Theory]
        [InlineData(651)]
        [InlineData(1000)]
        public void Description_LengthValidation_FailsForTooLong(int length)
        {
            // Arrange
            var desc = new string('A', length);
            var editGroup = new EditGroup("Group", "image.jpg", desc);

            // Act
            var results = ValidateModel(editGroup);

            // Assert
            Assert.Contains(results, r =>
                r.MemberNames.Contains(nameof(EditGroup.Description)) &&
                r.ErrorMessage == "Group description length must be between 1 and 650 characters.");
        }
        
        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }
    }
}
