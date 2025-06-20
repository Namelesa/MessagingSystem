using FluentAssertions;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Dto;

public class GroupMembersDtoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var dto = new GroupMembersDto();

        // Assert
        dto.Users.Should().NotBeNull();
        dto.Users.Should().BeEmpty();
    }

    [Fact]
    public void Users_Getter_ShouldReturnCorrectValue()
    {
        // Arrange
        var dto = new GroupMembersDto();
        var users = new List<string> { "user1", "user2", "user3" };

        // Act
        dto.Users = users;

        // Assert
        dto.Users.Should().BeSameAs(users);
        dto.Users.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void Users_Setter_ShouldSetCorrectValue()
    {
        // Arrange
        var dto = new GroupMembersDto();
        var users = new List<string> { "user1", "user2" };

        // Act
        dto.Users = users;

        // Assert
        dto.Users.Should().BeSameAs(users);
        dto.Users.Should().HaveCount(2);
    }
    
    [Fact]
    public void Users_Setter_WithNull_ShouldSetNull()
    {
        // Arrange
        var dto = new GroupMembersDto
        {
            // Act
            Users = null!
        };

        // Assert
        dto.Users.Should().BeNull();
    }
    

    [Fact]
    public void Users_Setter_WithEmptyList_ShouldSetEmptyList()
    {
        // Arrange
        var dto = new GroupMembersDto();
        var emptyUsers = new List<string>();

        // Act
        dto.Users = emptyUsers;

        // Assert
        dto.Users.Should().BeSameAs(emptyUsers);
        dto.Users.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldHavePublicGettersAndSetters()
    {
        // Arrange
        var usersProperty = typeof(GroupMembersDto).GetProperty(nameof(GroupMembersDto.Users));

        // Assert
        usersProperty.Should().NotBeNull();
        usersProperty.CanRead.Should().BeTrue();
        usersProperty.CanWrite.Should().BeTrue();
        usersProperty.GetGetMethod().Should().NotBeNull();
        usersProperty.GetSetMethod().Should().NotBeNull();
        usersProperty.GetGetMethod()?.IsPublic.Should().BeTrue();
        usersProperty.GetSetMethod()?.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void ObjectInitializer_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var dto = new GroupMembersDto
        {
            Users = ["user1", "user2", "user3"],
        };

        // Assert
        dto.Users.Should().ContainInOrder("user1", "user2", "user3");
    }

    [Fact]
    public void Users_DefaultInitialization_ShouldBeEmptyList()
    {
        // Act
        var dto = new GroupMembersDto();

        // Assert
        dto.Users.Should().NotBeNull();
        dto.Users.Should().BeOfType<List<string>>();
        dto.Users.Should().BeEmpty();
    }

    [Fact]
    public void Users_CanBeModifiedAfterInitialization()
    {
        // Arrange
        var dto = new GroupMembersDto();

        // Act
        dto.Users.Add("user1");
        dto.Users.Add("user2");

        // Assert
        dto.Users.Should().HaveCount(2);
        dto.Users.Should().Contain("user1");
        dto.Users.Should().Contain("user2");
    }

    [Fact]
    public void MultipleAssignments_ShouldWorkCorrectly()
    {
        // Arrange
        var dto = new GroupMembersDto
        {
            // Act & Assert
        };
        
        dto.Users = ["user1"];
        dto.Users.Should().ContainSingle("user1");

        dto.Users = ["user2", "user3"];
        dto.Users.Should().HaveCount(2);
        dto.Users.Should().NotContain("user1");
    }
    
    [Fact]
    public void Users_WithVariousLists_ShouldSetCorrectly()
    {
        // Arrange
        var dto = new GroupMembersDto();
        var testCases = new[]
        {
            [],
            ["single"],
            ["user1", "user2"],
            ["user1", "user2", "user3", "user4", "user5"],
            new List<string> { "", "empty", null! }
        };

        foreach (var testCase in testCases)
        {
            // Act
            dto.Users = testCase;

            // Assert
            dto.Users.Should().BeSameAs(testCase);
            dto.Users.Should().BeEquivalentTo(testCase);
        }
    }

    [Fact]
    public void ToString_ShouldReturnClassNameByDefault()
    {
        // Arrange
        var dto = new GroupMembersDto();

        // Act
        var result = dto.ToString();

        // Assert
        result.Should().Contain(nameof(GroupMembersDto));
    }
}