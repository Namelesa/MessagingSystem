using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core.Oto;

public class MessageTests
{
    [Fact]
    public void Constructor_SetsAllRequiredProperties()
    {
        // Arrange
        const string sender = "Alice";
        const string recipient = "Bob";
        const string content = "Hello";
        
        // Act
        var message = new Message(sender, recipient, content);
        
        // Assert
        Assert.Equal(sender, message.Sender);
        Assert.Equal(recipient, message.Recipient);
        Assert.Equal(content, message.Content);
        Assert.True((DateTime.UtcNow - message.SendTime).TotalSeconds < 1);
    }
    
    [Fact]
    public void Id_Is_Empty_By_Default()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");

        // Assert
        Assert.Equal(Guid.Empty, message.Id);
    }
    
    [Fact]
    public void Id_Can_Be_Set_Through_Init()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        
        // Act
        var message = new Message("Alice", "Bob", "Hello") { Id = expectedId };
        
        // Assert
        Assert.Equal(expectedId, message.Id);
    }
    
    [Fact]
    public void SendTime_Is_UTC_Now()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");

        // Assert
        Assert.True((DateTime.UtcNow - message.SendTime).TotalSeconds < 1);
    }
    
    [Fact]
    public void SendTime_Can_Be_Set_Through_Init()
    {
        // Arrange
        var expectedTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        
        // Act
        var message = new Message("Alice", "Bob", "Hello") { SendTime = expectedTime };
        
        // Assert
        Assert.Equal(expectedTime, message.SendTime);
    }
    
    [Fact]
    public void Sender_Property_Returns_Constructor_Value()
    {
        // Arrange
        const string expectedSender = "TestSender";
        
        // Act
        var message = new Message(expectedSender, "Bob", "Hello");
        
        // Assert - Multiple accesses to ensure getter coverage
        Assert.Equal(expectedSender, message.Sender);
        var senderFromProperty = message.Sender;
        Assert.Equal(expectedSender, senderFromProperty);
        Assert.NotNull(message.Sender);
    }
    
    [Fact]
    public void Recipient_Property_Returns_Constructor_Value()
    {
        // Arrange
        const string expectedRecipient = "TestRecipient";
        
        // Act
        var message = new Message("Alice", expectedRecipient, "Hello");
        
        // Assert - Multiple accesses to ensure getter coverage
        Assert.Equal(expectedRecipient, message.Recipient);
        var recipientFromProperty = message.Recipient;
        Assert.Equal(expectedRecipient, recipientFromProperty);
        Assert.NotNull(message.Recipient);
    }
    
    [Fact]
    public void SenderHash_Is_Null_By_Default()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");
        
        // Assert
        Assert.Null(message.SenderHash);
    }
    
    [Fact]
    public void RecipientHash_Is_Null_By_Default()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");
        
        // Assert
        Assert.Null(message.RecipientHash);
    }
    
    [Fact]
    public void ReplyFor_Is_Null_By_Default()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");
        
        // Assert
        Assert.Null(message.ReplyFor);
    }
    
    [Fact]
    public void IsDeleted_Is_False_By_Default()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");
        
        // Assert
        Assert.False(message.IsDeleted);
    }
    
    [Fact]
    public void IsEdited_Is_False_By_Default()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");
        
        // Assert
        Assert.False(message.IsEdited);
    }
    
    [Fact]
    public void EditDate_Is_Null_By_Default()
    {
        // Arrange & Act
        var message = new Message("Alice", "Bob", "Hello");
        
        // Assert
        Assert.Null(message.EditDate);
    }
    
    [Fact]
    public void SoftDeleteInfo_Sets_IsDeleted_To_True()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");

        // Act
        message.SoftDeleteInfo();

        // Assert
        Assert.True(message.IsDeleted);
    }
    
    [Fact]
    public void EditInfo_Updates_Content_And_Flags()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        const string newContent = "New content";

        // Act
        message.EditInfo(newContent);

        // Assert
        Assert.True(message.IsEdited);
        Assert.Equal(newContent, message.Content);
        Assert.True(message.EditDate.HasValue);
        Assert.True((DateTime.UtcNow - message.EditDate.Value).TotalSeconds < 1);
    }
    
    [Fact]
    public void EditInfo_With_Empty_String_Updates_Content()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        const string newContent = "";

        // Act
        message.EditInfo(newContent);

        // Assert
        Assert.True(message.IsEdited);
        Assert.Equal(newContent, message.Content);
        Assert.True(message.EditDate.HasValue);
    }
    
    [Fact]
    public void EditInfo_With_Null_String_Updates_Content()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        const string newContent = null;

        // Act
        message.EditInfo(newContent);

        // Assert
        Assert.True(message.IsEdited);
        Assert.Equal(newContent, message.Content);
        Assert.True(message.EditDate.HasValue);
    }
    
    [Fact]
    public void Reply_Sets_ReplyFor()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        var replyId = Guid.NewGuid();

        // Act
        message.Reply(replyId);

        // Assert
        Assert.Equal(replyId, message.ReplyFor);
    }
    
    [Fact]
    public void Reply_With_Empty_Guid_Sets_ReplyFor()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        var replyId = Guid.Empty;

        // Act
        message.Reply(replyId);

        // Assert
        Assert.Equal(replyId, message.ReplyFor);
    }
    
    [Fact]
    public void SetHashes_Sets_SenderHash_And_RecipientHash()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        const string senderHash = "hashedSender";
        const string recipientHash = "hashedRecipient";

        // Act
        message.SetHashes(senderHash, recipientHash);

        // Assert
        Assert.Equal(senderHash, message.SenderHash);
        Assert.Equal(recipientHash, message.RecipientHash);
    }
    
    [Fact]
    public void SetHashes_With_Null_Values_Sets_Hashes()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");

        // Act
        message.SetHashes(null, null);

        // Assert
        Assert.Null(message.SenderHash);
        Assert.Null(message.RecipientHash);
    }
    
    [Fact]
    public void SetHashes_With_Empty_Strings_Sets_Hashes()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");

        // Act
        message.SetHashes("", "");

        // Assert
        Assert.Equal("", message.SenderHash);
        Assert.Equal("", message.RecipientHash);
    }
    
    [Fact]
    public void Multiple_Operations_Work_Together()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        var replyId = Guid.NewGuid();
        const string newContent = "Edited content";
        const string senderHash = "hash1";
        const string recipientHash = "hash2";

        // Act
        message.Reply(replyId);
        message.EditInfo(newContent);
        message.SetHashes(senderHash, recipientHash);
        message.SoftDeleteInfo();

        // Assert
        Assert.Equal(replyId, message.ReplyFor);
        Assert.Equal(newContent, message.Content);
        Assert.True(message.IsEdited);
        Assert.True(message.EditDate.HasValue);
        Assert.Equal(senderHash, message.SenderHash);
        Assert.Equal(recipientHash, message.RecipientHash);
        Assert.True(message.IsDeleted);
    }
    
    [Xunit.Theory]
    [InlineData("Alice", "Bob", "Hello")]
    [InlineData("", "", "")]
    [InlineData("User1", "User2", "Test message")]
    [InlineData(null, null, null)]
    public void Constructor_WithDifferentParameters_SetsPropertiesCorrectly(string sender, string recipient, string content)
    {
        // Act
        var message = new Message(sender, recipient, content);
        
        // Assert
        Assert.Equal(sender, message.Sender);
        Assert.Equal(recipient, message.Recipient);
        Assert.Equal(content, message.Content);
        
        // This ensures both getter and setter are covered
        var senderValue = message.Sender;
        var recipientValue = message.Recipient;
        
        Assert.Equal(sender, senderValue);
        Assert.Equal(recipient, recipientValue);
    }
    
    [Fact]
    public void Sender_Getter_Coverage_Test()
    {
        // Arrange
        var message1 = new Message("Alice", "Bob", "Hello");
        var message2 = new Message(null, "Bob", "Hello");
        var message3 = new Message("", "Bob", "Hello");
        
        // Act & Assert
        Assert.Equal("Alice", message1.Sender);
        Assert.Null(message2.Sender);
        Assert.Equal("", message3.Sender);
    }
    
    [Fact]
    public void Recipient_Getter_Coverage_Test()
    {
        // Arrange
        var message1 = new Message("Alice", "Bob", "Hello");
        var message2 = new Message("Alice", null, "Hello");
        var message3 = new Message("Alice", "", "Hello");
        
        // Act & Assert
        Assert.Equal("Bob", message1.Recipient);
        Assert.Null(message2.Recipient);
        Assert.Equal("", message3.Recipient);
    }

    [Fact]
    public void Sender_PrivateSetter_Coverage_Through_Reflection()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        var senderProperty = typeof(Message).GetProperty("Sender");
        
        // Act
        senderProperty?.SetValue(message, "ReflectionSender");
        
        // Assert
        Assert.Equal("ReflectionSender", message.Sender);
    }
    
    [Fact]
    public void Recipient_PrivateSetter_Coverage_Through_Reflection()
    {
        // Arrange
        var message = new Message("Alice", "Bob", "Hello");
        var recipientProperty = typeof(Message).GetProperty("Recipient");
        
        // Act
        recipientProperty?.SetValue(message, "ReflectionRecipient");
        
        // Assert
        Assert.Equal("ReflectionRecipient", message.Recipient);
    }
}