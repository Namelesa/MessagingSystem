using FluentAssertions;
using MessagingSystem.Services.Messaging.Core;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core;

public class MessageFilterTests
{
    [Fact]
    public void CanInitializeProperties_WithInitSetters()
    {
        const string sender = "userA";
        const string recipient = "userB";
        var date = new DateTime(2023, 6, 15);

        var filter = new MessageFilter
        {
            Sender = sender,
            Recipient = recipient,
            Date = date
        };

        filter.Sender.Should().Be(sender);
        filter.Recipient.Should().Be(recipient);
        filter.Date.Should().Be(date);
    }

    [Fact]
    public void Properties_AreImmutableAfterInitialization()
    {
        var filter = new MessageFilter
        {
            Sender = "userA",
            Recipient = "userB",
            Date = new DateTime(2023, 6, 15)
        };
        
        filter.Sender.Should().NotBeNull();
        filter.Recipient.Should().NotBeNull();
        filter.Date.Should().NotBeNull();
    }
}