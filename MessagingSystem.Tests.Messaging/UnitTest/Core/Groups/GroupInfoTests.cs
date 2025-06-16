using Xunit;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core.Groups
{
    public class GroupInfoTests
    {
        [Fact]
        public void EditInfo_Updates_Values()
        {
            // Arrange
            var group = new GroupInfo("OldName", "old.jpg", "OldDesc", "admin");

            // Act
            group.EditInfo("NewName", "new.jpg", "NewDesc", "Hash");

            // Assert
            Assert.Equal("NewName", group.GroupName);
            Assert.Equal("new.jpg", group.Image);
            Assert.Equal("NewDesc", group.Description);
            Assert.Equal("Hash", group.GroupNameHash);
        }
        
        [Fact]
        public void AddUser_Adds_Member()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            // Act
            group.AddUser("User1");

            // Assert
            Assert.Single(group.Members);
            Assert.Equal("User1", group.Members[0].UserNickName);
        }
        
        [Fact]
        public void AddUser_Does_Not_Add_Duplicate()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            group.AddUser("User1");

            // Act
            group.AddUser("User1");

            // Assert
            Assert.Single(group.Members);
        }
        
        [Fact]
        public void AddUser_Throws_If_Full()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            for (int i = 0; i < 40; i++) 
            {
                group.AddUser($"User{i}");

            }
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => group.AddUser("User41"));
        }
        
        [Fact]
        public void AddUsers_Adds_Multiple()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            // Act
            group.AddUsers(new[] { "User1", "User2" });

            // Assert
            Assert.Equal(2, group.Members.Count);
        }
        
        [Fact]
        public void AddUsers_Skips_Empty_And_Duplicates()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            // Act
            group.AddUsers(new[] { "User1", " ", "User1" });

            // Assert
            Assert.Single(group.Members);
        }
        
        [Fact]
        public void ApplyHashToMembers_SetsHash()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            group.AddUser("User1");

            // Act
            group.ApplyHashToMembers(s => $"Hash:{s}");

            // Assert
            Assert.Equal("Hash:User1", group.Members[0].UserNickNameHash);
        }
        
        [Fact]
        public void EncryptMembers_EncryptsNames()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            group.AddUser("User1");

            // Act
            group.EncryptMembers(s => $"Encrypt:{s}");

            // Assert
            Assert.Equal("Encrypt:User1", group.Members[0].UserNickName);
        }
        
        [Fact]
        public void DeleteUsers_Removes_Matched()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            group.AddUser("User1");

            // Act
            group.DeleteUsers(new[] { "User1" });

            // Assert
            Assert.Empty(group.Members);
        }
        
        [Fact]
        public void SetHash_Updates_AdminHash_And_GroupHash()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            // Act
            group.SetHash("adminHash", "groupHash");

            // Assert
            Assert.Equal("adminHash", group.AdminHash);
            Assert.Equal("groupHash", group.GroupNameHash);
        }
        
        [Fact]
        public void SetAdminHash_Updates_Only_AdminHash()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            // Act
            group.SetAdminHash("newHash");

            // Assert
            Assert.Equal("newHash", group.AdminHash);
        }
        
        [Fact]
        public void EditAdminNick_Updates_Admin()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            // Act
            group.EditAdminNick("newadmin");

            // Assert
            Assert.Equal("newadmin", group.Admin);
        }
        
        [Fact]
        public void SetMembersImages_Updates_Member_Images()
        {
            // Arrange
            var group = new GroupInfo("Group", "img.jpg", "desc", "admin");

            group.AddUser("User1");

            // Act
            group.SetMembersImages(new Dictionary<string, string>{{"User1","new.jpg"}});

            // Assert
            Assert.Equal("new.jpg", group.Members[0].Image);
        }
        
        [Fact]
        public void RowVersion_Can_Be_Set_Using_Reflection()
        {
            // Arrange
            var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

            // Act - set private set property with reflection
            var rowVersionProperty = typeof(GroupInfo).GetProperty("RowVersion")!;
            var newVersion = new byte[] { 1, 2, 3 };
            rowVersionProperty.SetValue(group, newVersion);

            // Assert
            Assert.Equal(newVersion, group.RowVersion);
        }
        
        [Fact]
        public void Id_Can_Be_Set_Using_Reflection()
        {
            // Arrange
            var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

            // Act - set private init property with reflection
            var idProperty = typeof(GroupInfo).GetProperty("Id")!;
            idProperty.SetValue(group, Guid.NewGuid());

            // Assert
            Assert.NotEqual(Guid.Empty, group.Id);
        }
        
        [Fact]
        public void Members_Can_Be_Set_Using_Reflection()
        {
            // Arrange
            var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

            var membersProperty = typeof(GroupInfo).GetProperty("Members");

            var newList = new List<MessagingSystem.Services.Messaging.Core.Groups.GroupMember.GroupMembers>
            {
                new("NewMember")
            };
    
            // Act
            membersProperty?.SetValue(group, newList);

            // Assert
            Assert.Single(group.Members);
            Assert.Equal("NewMember", group.Members[0].UserNickName);
        }

        [Fact]
    public void AddUsers_Adds_New_Members()
    {
        // Arrange
        var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

        // Act
        group.AddUsers(new[] { "Alice", "Bob" });

        // Assert
        Assert.Equal(2, group.Members.Count);
        Assert.Contains("Alice", group.Members[0].UserNickName);
        Assert.Contains("Bob", group.Members[1].UserNickName);
    }
    
    [Fact]
    public void AddUsers_Skips_Empty_Or_Null_Names()
    {
        // Arrange
        var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

        // Act
        group.AddUsers(new[] { "Charlie", "", "  ", null });

        // Assert
        Assert.Single(group.Members);
        Assert.Equal("Charlie", group.Members[0].UserNickName);
    }
    
    [Fact]
    public void AddUsers_Does_Not_Add_Duplicates()
    {
        // Arrange
        var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

        // Act
        group.AddUsers(new[] { "Charlie", "Charlie" });

        // Assert
        Assert.Single(group.Members);
        Assert.Equal("Charlie", group.Members[0].UserNickName);
    }
    
    [Fact]
    public void AddUsers_Throws_If_MaxMembers_Exceeded()
    {
        // Arrange
        var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

        // Fill up to max first
        for (int i = 0; i < 40; i++)
        {
            group.AddUsers(new[] { $"User{i}" });
        }
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => group.AddUsers(new[] { "OneMore" }))
            .Message.Contains("Maximum number of group members exceeded");
    }
    
    [Fact]
    public void AddUsers_Ignores_Existing_Members()
    {
        // Arrange
        var group = new GroupInfo("Test", "img.jpg", "desc", "admin");

        group.AddUsers(new[] { "Alice" });

        // Act
        group.AddUsers(new[] { "Alice", "Charlie" });

        // Assert
        Assert.Equal(2, group.Members.Count);
        Assert.Contains("Charlie", group.Members[1].UserNickName);
    }
        
    }
}

