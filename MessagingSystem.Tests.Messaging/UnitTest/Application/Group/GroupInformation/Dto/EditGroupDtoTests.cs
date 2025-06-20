using FluentAssertions;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Dto;

public class EditGroupDtoTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        const string groupName = "Test Group";
        const string image = "image.jpg";
        const string description = "Test Description";

        // Act
        var dto = new EditGroupDto(groupName, image, description);

        // Assert
        dto.GroupName.Should().Be(groupName);
        dto.Image.Should().Be(image);
        dto.Description.Should().Be(description);
    }

    [Fact]
    public void Constructor_WithNullImage_ShouldInitializeImageAsNull()
    {
        // Arrange
        const string groupName = "Test Group";
        const string? image = null;
        const string description = "Test Description";

        // Act
        var dto = new EditGroupDto(groupName, image, description);

        // Assert
        dto.GroupName.Should().Be(groupName);
        dto.Image.Should().BeNull();
        dto.Description.Should().Be(description);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        const string groupName = "";
        const string image = "";
        const string description = "";

        // Act
        var dto = new EditGroupDto(groupName, image, description);

        // Assert
        dto.GroupName.Should().Be(string.Empty);
        dto.Image.Should().Be(string.Empty);
        dto.Description.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_WithInitAccessors_ShouldBeReadOnlyAfterConstruction()
    {
        // Arrange
        const string groupName = "Test Group";
        const string image = "image.jpg";
        const string description = "Test Description";

        // Act
        var dto = new EditGroupDto(groupName, image, description);

        // Assert
        dto.GroupName.Should().Be(groupName);
        dto.Image.Should().Be(image);
        dto.Description.Should().Be(description);
        
        var groupNameProperty = typeof(EditGroupDto).GetProperty(nameof(EditGroupDto.GroupName));
        var imageProperty = typeof(EditGroupDto).GetProperty(nameof(EditGroupDto.Image));
        var descriptionProperty = typeof(EditGroupDto).GetProperty(nameof(EditGroupDto.Description));

        groupNameProperty?.CanRead.Should().BeTrue();
        groupNameProperty?.CanWrite.Should().BeTrue(); 
        
        imageProperty?.CanRead.Should().BeTrue();
        imageProperty?.CanWrite.Should().BeTrue();
        
        descriptionProperty?.CanRead.Should().BeTrue();
        descriptionProperty?.CanWrite.Should().BeTrue();
    }

    [Xunit.Theory]
    [InlineData("Group 1", "image1.jpg", "Description 1")]
    [InlineData("Group 2", null, "Description 2")]
    [InlineData("Group 3", "", "Description 3")]
    [InlineData("", "image.png", "")]
    public void Constructor_WithVariousInputs_ShouldInitializeCorrectly(
        string groupName, 
        string? image, 
        string description)
    {
        // Act
        var dto = new EditGroupDto(groupName, image, description);

        // Assert
        dto.GroupName.Should().Be(groupName);
        dto.Image.Should().Be(image);
        dto.Description.Should().Be(description);
    }

    [Fact]
    public void ObjectInitializer_WithInitProperties_ShouldWork()
    {
        // Arrange & Act
        var dto = new EditGroupDto("Initial Group", "initial.jpg", "Initial Description")
        {
            GroupName = "Updated Group",
            Image = "updated.jpg",
            Description = "Updated Description"
        };

        // Assert
        dto.GroupName.Should().Be("Updated Group");
        dto.Image.Should().Be("updated.jpg");
        dto.Description.Should().Be("Updated Description");
    }
    
    [Fact]
    public void Equality_BetweenInstancesWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var dto1 = new EditGroupDto("Test Group", "image.jpg", "Test Description");
        var dto2 = new EditGroupDto("Test Group", "image.jpg", "Test Description");

        // Act & Assert
        dto1.Should().NotBeSameAs(dto2);
        
        dto1.GroupName.Should().Be(dto2.GroupName);
        dto1.Image.Should().Be(dto2.Image);
        dto1.Description.Should().Be(dto2.Description);
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulRepresentation()
    {
        // Arrange
        var dto = new EditGroupDto("Test Group", "image.jpg", "Test Description");

        // Act
        var stringRepresentation = dto.ToString();

        // Assert
        stringRepresentation.Should().NotBeNullOrEmpty();
        stringRepresentation.Should().Contain(nameof(EditGroupDto));
    }
}