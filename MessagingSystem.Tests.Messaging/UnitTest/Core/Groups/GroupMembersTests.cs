using MessagingSystem.Services.Messaging.Core.Groups.Group;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMember;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core.Groups;

public class GroupMembersTests
{
    [Fact]
    public void Initializes_Correctly()
    {
        // Act
        var member = new GroupMembers("Alice");

        // Assert
        Assert.Equal("Alice", member.UserNickName);
        Assert.True((DateTime.UtcNow - member.JoinedTime).TotalSeconds < 5);
        Assert.Equal(Guid.Empty, member.GroupId);
        Assert.Null(member.Group);
        Assert.Null(member.Image);
        Assert.Null(member.UserNickNameHash);
    }
    
    [Fact]
    public void SetHash_Updates_UserNickNameHash()
    {
        // Arrange
        var member = new GroupMembers("Alice");

        // Act
        member.SetHash("hashedUsername");

        // Assert
        Assert.Equal("hashedUsername", member.UserNickNameHash);
    }
    
    [Fact]
    public void SetImage_Updates_Image()
    {
        // Arrange
        var member = new GroupMembers("Alice");

        // Act
        member.SetImage("http://example.com/image.jpg");

        // Assert
        Assert.Equal("http://example.com/image.jpg", member.Image);
    }
    
    [Fact]
    public void UserNickName_Can_Be_Reset()
    {
        // Arrange
        var member = new GroupMembers("Alice")
        {
            // Act
            UserNickName = "Charlie"
        };

        // Assert
        Assert.Equal("Charlie", member.UserNickName);
    }
    
    [Fact]
    public void Id_Can_Be_Initialized()
    {
        // Act
        var id = Guid.NewGuid();

        var member = new GroupMembers("Alice")
        {
            Id = id
        };
        
        // Assert
        Assert.Equal(id, member.Id);
    }
    
    [Fact]
    public void GroupId_Can_Be_Initialized()
    {
        // Act
        var groupId = Guid.NewGuid();

        var member = new GroupMembers("Alice")
        {
            GroupId = groupId
        };
        
        // Assert
        Assert.Equal(groupId, member.GroupId);
    }
    
    [Fact]
    public void Group_Can_Be_Initialized()
    {
        // Arrange
        var group = new GroupInfo("Test Group", "image.jpg", "desc", "admin");

        // Act
        var member = new GroupMembers("Alice")
        {
            Group = group
        };
    
        // Assert
        Assert.Equal(group, member.Group);
    }
    
    [Fact]
    public void JoinedTime_Can_Be_Initialized()
    {
        // Arrange
        var customTime = new DateTime(2025, 6, 16, 15, 0, 0, DateTimeKind.Utc);

        // Act
        var member = new GroupMembers("Alice")
        {
            JoinedTime = customTime
        };
    
        // Assert
        Assert.Equal(customTime, member.JoinedTime);
    }
}