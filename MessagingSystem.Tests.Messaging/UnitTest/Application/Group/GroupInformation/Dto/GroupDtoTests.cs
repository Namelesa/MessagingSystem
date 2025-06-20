using FluentAssertions;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Dto;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation.Dto;

public class GroupDtoTests
{
    private readonly byte[] _testRowVersion = [1, 2, 3, 4, 5];
    private readonly List<string> _testUsers = ["user1", "user2", "user3"];

    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        const string groupName = "Test Group";
        const string image = "image.jpg";
        const string description = "Test Description";
        const string admin = "admin1";
        var users = new List<string> { "user1", "user2" };
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        // Act
        var dto = new GroupDto(groupName, image, description, admin, users, rowVersion);

        // Assert
        dto.GroupName.Should().Be(groupName);
        dto.Image.Should().Be(image);
        dto.Description.Should().Be(description);
        dto.Admin.Should().Be(admin);
        dto.Users.Should().BeEquivalentTo(users);
        dto.Members.Should().BeNull(); 
        dto.RowVersion.Should().Be(Convert.ToBase64String(rowVersion));
    }

    [Fact]
    public void Constructor_WithNullImage_ShouldInitializeImageAsNull()
    {
        // Arrange & Act
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, _testRowVersion);

        // Assert
        dto.Image.Should().BeNull();
        dto.GroupName.Should().Be("Group");
        dto.Description.Should().Be("Description");
    }

    [Fact]
    public void InitProperties_ShouldBeSetOnlyDuringInitialization()
    {
        // Arrange & Act
        var dto = new GroupDto("Initial Group", "initial.jpg", "Initial Description", "admin", _testUsers, _testRowVersion)
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
    public void InitProperties_ShouldHaveCorrectAccessors()
    {
        // Arrange
        var groupNameProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.GroupName));
        var imageProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Image));
        var descriptionProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Description));

        // Assert
        groupNameProperty?.CanRead.Should().BeTrue();
        groupNameProperty?.CanWrite.Should().BeTrue();
        
        imageProperty?.CanRead.Should().BeTrue();
        imageProperty?.CanWrite.Should().BeTrue();
        
        descriptionProperty?.CanRead.Should().BeTrue();
        descriptionProperty?.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void PrivateSetProperties_ShouldHaveCorrectAccessors()
    {
        // Arrange
        var adminProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Admin));
        var usersProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Users));
        var membersProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Members));

        // Assert
        adminProperty?.CanRead.Should().BeTrue();
        adminProperty?.CanWrite.Should().BeTrue();
        adminProperty?.GetSetMethod(true).Should().NotBeNull(); 
        adminProperty?.GetSetMethod(false).Should().BeNull(); 

        usersProperty?.CanRead.Should().BeTrue();
        usersProperty?.CanWrite.Should().BeTrue();
        usersProperty?.GetSetMethod(true).Should().NotBeNull();
        usersProperty?.GetSetMethod(false).Should().BeNull();

        membersProperty?.CanRead.Should().BeTrue();
        membersProperty?.CanWrite.Should().BeTrue();
        membersProperty?.GetSetMethod(true).Should().NotBeNull();
        membersProperty?.GetSetMethod(false).Should().BeNull();
    }

    [Fact]
    public void RowVersion_ShouldBeReadOnlyAndConvertToBase64()
    {
        // Arrange
        var rowVersion = new byte[] { 255, 128, 64, 32, 16, 8, 4, 2, 1 };
        var expectedBase64 = Convert.ToBase64String(rowVersion);

        // Act
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, rowVersion);

        // Assert
        dto.RowVersion.Should().Be(expectedBase64);
        
        var rowVersionProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.RowVersion));
        rowVersionProperty?.CanRead.Should().BeTrue();
        rowVersionProperty?.CanWrite.Should().BeFalse();
        rowVersionProperty?.GetSetMethod(true).Should().BeNull();
    }

    [Fact]
    public void AddAdminLikeUser_ShouldUpdateAdminAndAddToUsers()
    {
        // Arrange
        var dto = new GroupDto("Group", null, "Description", "oldAdmin", ["user1"], _testRowVersion);
        const string newAdmin = "newAdmin";

        // Act
        dto.AddAdminLikeUser(newAdmin);

        // Assert
        dto.Admin.Should().Be(newAdmin);
        dto.Users.Should().Contain(newAdmin);
        dto.Users.Should().HaveCount(2);
    }

    [Fact]
    public void AddAdminLikeUser_WhenAdminAlreadyInUsers_ShouldStillAddAdmin()
    {
        // Arrange
        var users = new List<string> { "user1", "existingAdmin" };
        var dto = new GroupDto("Group", null, "Description", "oldAdmin", users, _testRowVersion);

        // Act
        dto.AddAdminLikeUser("existingAdmin");

        // Assert
        dto.Admin.Should().Be("existingAdmin");
        dto.Users.Should().HaveCount(3); 
        dto.Users.Count(u => u == "existingAdmin").Should().Be(2);
    }

    [Fact]
    public void SetMembers_ShouldUpdateMembersProperty()
    {
        // Arrange
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, _testRowVersion);
        var members = new List<UserInGroupDto>
        {
            new("user1", "avatar1.jpg"),
            new("user2")
        };

        // Act
        dto.SetMembers(members);

        // Assert
        dto.Members.Should().NotBeNull();
        dto.Members.Should().BeEquivalentTo(members);
        dto.Members.Should().HaveCount(2);
    }

    [Fact]
    public void SetMembers_WithEmptyList_ShouldSetEmptyMembers()
    {
        // Arrange
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, _testRowVersion);
        var emptyMembers = new List<UserInGroupDto>();

        // Act
        dto.SetMembers(emptyMembers);

        // Assert
        dto.Members.Should().NotBeNull();
        dto.Members.Should().BeEmpty();
    }

    [Xunit.Theory]
    [InlineData("Group 1", "image1.jpg", "Description 1", "admin1")]
    [InlineData("Group 2", null, "Description 2", "admin2")]
    [InlineData("", "", "", "")]
    public void Constructor_WithVariousInputs_ShouldInitializeCorrectly(
        string groupName, 
        string? image, 
        string description,
        string admin)
    {
        // Arrange
        var users = new List<string> { "user1", "user2" };
        var rowVersion = new byte[] { 1, 2, 3 };

        // Act
        var dto = new GroupDto(groupName, image, description, admin, users, rowVersion);

        // Assert
        dto.GroupName.Should().Be(groupName);
        dto.Image.Should().Be(image);
        dto.Description.Should().Be(description);
        dto.Admin.Should().Be(admin);
        dto.Users.Should().BeEquivalentTo(users);
    }

    [Fact]
    public void Users_ShouldBeMutableThroughReference()
    {
        // Arrange
        var users = new List<string> { "user1" };
        var dto = new GroupDto("Group", null, "Description", "admin", users, _testRowVersion);

        // Act 
        dto.Users.Add("user2");

        // Assert
        dto.Users.Should().HaveCount(2);
        dto.Users.Should().Contain("user2");
        
        users.Should().HaveCount(2);
        users.Should().Contain("user2");
    }

    [Fact]
    public void ComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange
        var dto = new GroupDto("Test Group", "group.jpg", "Test Description", "admin1",
            ["user1", "user2"], [1, 2, 3, 4, 5]);

        var members = new List<UserInGroupDto>
        {
            new("admin1", "admin.jpg"),
            new("user1"),
            new("user2", "user2.jpg")
        };

        // Act
        dto.SetMembers(members);
        dto.AddAdminLikeUser("newAdmin");

        // Assert
        dto.GroupName.Should().Be("Test Group");
        dto.Admin.Should().Be("newAdmin");
        dto.Users.Should().HaveCount(3);
        dto.Users.Should().Contain("newAdmin");
        dto.Members.Should().HaveCount(3);
        dto.RowVersion.Should().Be(Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Fact]
    public void RowVersion_WithDifferentByteArrays_ShouldProduceDifferentBase64()
    {
        // Arrange
        var rowVersion1 = new byte[] { 1, 2, 3 };
        var rowVersion2 = new byte[] { 4, 5, 6 };

        // Act
        var dto1 = new GroupDto("Group", null, "Description", "admin", _testUsers, rowVersion1);
        var dto2 = new GroupDto("Group", null, "Description", "admin", _testUsers, rowVersion2);

        // Assert
        dto1.RowVersion.Should().NotBe(dto2.RowVersion);
        dto1.RowVersion.Should().Be(Convert.ToBase64String(rowVersion1));
        dto2.RowVersion.Should().Be(Convert.ToBase64String(rowVersion2));
    }

    [Fact]
    public void PrivateSetters_ShouldNotBeAccessibleExternally()
    {
        // Act & Assert
        var adminProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Admin));
        var usersProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Users));
        var membersProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Members));
        
        var adminSetter = adminProperty?.GetSetMethod(true);
        var usersSetter = usersProperty?.GetSetMethod(true);
        var membersSetter = membersProperty?.GetSetMethod(true);

        adminSetter.Should().NotBeNull();
        adminSetter.IsPrivate.Should().BeTrue();

        usersSetter.Should().NotBeNull();
        usersSetter.IsPrivate.Should().BeTrue();

        membersSetter.Should().NotBeNull();
        membersSetter.IsPrivate.Should().BeTrue();
    }

    [Fact]  
    public void PrivateSetters_ShouldBeInvokableViaReflection()
    {
        // Arrange
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, _testRowVersion);
        var newUsers = new List<string> { "newUser1", "newUser2" };
        var newMembers = new List<UserInGroupDto> { new("test") };

        // Act
        var adminProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Admin));
        var usersProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Users));
        var membersProperty = typeof(GroupDto).GetProperty(nameof(GroupDto.Members));

        adminProperty?.SetValue(dto, "newAdmin");
        usersProperty?.SetValue(dto, newUsers);
        membersProperty?.SetValue(dto, newMembers);

        // Assert
        dto.Admin.Should().Be("newAdmin");
        dto.Users.Should().BeEquivalentTo(newUsers);
        dto.Members.Should().BeEquivalentTo(newMembers);
    }

    [Fact]
    public void PrivateSetters_Coverage_ThroughInternalMethods()
    {
        // Arrange
        var dto = new GroupDto("Group", null, "Description", "originalAdmin",
            ["user1"], _testRowVersion);
        
        var initialUsersCount = dto.Users.Count;
        var initialAdmin = dto.Admin;

        // Act
        dto.AddAdminLikeUser("newAdmin");
        
        var testMembers = new List<UserInGroupDto> 
        { 
            new("member1", "avatar1.jpg") 
        };
        dto.SetMembers(testMembers); 

        // Assert 
        dto.Admin.Should().Be("newAdmin").And.NotBe(initialAdmin);
        dto.Users.Should().HaveCount(initialUsersCount + 1);
        dto.Users.Should().Contain("newAdmin");
        dto.Members.Should().NotBeNull();
        dto.Members.Should().BeEquivalentTo(testMembers);
    }
    
    [Fact]
    public void GroupId_Get_ShouldReturnCorrectValue()
    {
        // Arrange
        var groupId = Guid.NewGuid();
    
        // Act
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, _testRowVersion, groupId);
    
        // Assert
        dto.GroupId.Should().Be(groupId);
    }

    [Fact]
    public void GroupId_Init_ShouldSetCorrectValue()
    {
        // Arrange
        var groupId = Guid.NewGuid();
    
        // Act
        var dto = new GroupDto("Group", null, "Description", "admin", _testUsers, _testRowVersion)
        {
            GroupId = groupId
        };
    
        // Assert
        dto.GroupId.Should().Be(groupId);
    }
}