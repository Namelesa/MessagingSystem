using System.Reflection;
using FluentAssertions;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core.Groups;

public class GroupMessageTests
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _messageId = Guid.NewGuid();
    private readonly string _sender = "user1";
    private readonly string _content = "Hello, world!";

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var msg = new GroupMessage(_sender, _content)
        {
            Id = _messageId,
            GroupId = _groupId,
            SendTime = new DateTime(2023, 1, 1),
            Group = null
        };

        msg.Id.Should().Be(_messageId);
        msg.GroupId.Should().Be(_groupId);
        msg.Sender.Should().Be(_sender);
        msg.Content.Should().Be(_content);
        msg.SendTime.Should().Be(new DateTime(2023, 1, 1));
        msg.Group.Should().BeNull();
        
        msg.ReplyFor.Should().BeNull();
        msg.SenderHash.Should().BeNull();
        msg.IsDeleted.Should().BeFalse();
        msg.IsEdited.Should().BeFalse();
        msg.EditTime.Should().BeNull();
    }

    [Fact]
    public void SoftDeleteInfo_ShouldSetIsDeletedTrue()
    {
        var msg = new GroupMessage(_sender, _content);

        msg.IsDeleted.Should().BeFalse();

        msg.SoftDeleteInfo();

        msg.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void EditInfo_ShouldUpdateContentAndSetEditedFlagAndEditTime()
    {
        var msg = new GroupMessage(_sender, _content);
        var newContent = "Edited content";

        msg.IsEdited.Should().BeFalse();
        msg.Content.Should().Be(_content);
        msg.EditTime.Should().BeNull();

        msg.EditInfo(newContent);

        msg.IsEdited.Should().BeTrue();
        msg.Content.Should().Be(newContent);
        msg.EditTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Reply_ShouldSetReplyFor()
    {
        var msg = new GroupMessage(_sender, _content);
        var replyId = Guid.NewGuid();

        msg.ReplyFor.Should().BeNull();

        msg.Reply(replyId);

        msg.ReplyFor.Should().Be(replyId);
    }

    [Fact]
    public void SetHashes_ShouldSetSenderHash()
    {
        var msg = new GroupMessage(_sender, _content);
        var hash = "hashstring";

        msg.SenderHash.Should().BeNull();

        msg.SetHashes(hash);

        msg.SenderHash.Should().Be(hash);
    }

    [Fact]
    public void Properties_WithInit_CanBeSetOnlyDuringInitialization()
    {
        var msg = new GroupMessage(_sender, _content)
        {
            Id = _messageId,
            GroupId = _groupId,
            SendTime = DateTime.UtcNow,
            Group = null
        };
        
        msg.Id.Should().Be(_messageId);
        msg.GroupId.Should().Be(_groupId);
        msg.SendTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public void Sender_PrivateSetter_CanBeChangedViaReflection()
    {
        var msg = new GroupMessage("originalSender", "content");
        
        msg.Sender.Should().Be("originalSender");
        
        var senderProperty = typeof(GroupMessage).GetProperty("Sender", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        senderProperty.Should().NotBeNull();
        
        senderProperty.SetValue(msg, "newSender");
        
        msg.Sender.Should().Be("newSender");
    }
}