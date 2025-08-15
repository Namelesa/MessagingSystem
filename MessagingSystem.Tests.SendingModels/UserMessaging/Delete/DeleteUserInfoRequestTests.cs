using MessagingSystem.SendingModels.UserMessaging.Delete;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.Delete;

public class DeleteUserInfoRequestTests
{
    [Fact]
    public void Constructor_WithValidUserNickNameHash_SetsPropertyCorrectly()
    {
        // Arrange
        var expectedHash = "abc123hash";
        var expectedNickName = "abc123nick";

        // Act
        var request = new DeleteUserInfoRequest(expectedHash, expectedNickName);

        // Assert
        Assert.Equal(expectedHash, request.UserNickNameHash);
        Assert.Equal(expectedNickName, request.UserNickName);
    }

    [Fact]
    public void Constructor_WithEmptyString_SetsPropertyToEmptyString()
    {
        // Arrange
        var emptyHash = string.Empty;
        var emptyNick = string.Empty;

        // Act
        var request = new DeleteUserInfoRequest(emptyHash, emptyNick);

        // Assert
        Assert.Equal(emptyHash, request.UserNickNameHash);
        Assert.Equal(emptyNick, request.UserNickName);
    }

    [Fact]
    public void Constructor_WithNullValue_SetsPropertyToNull()
    {
        // Arrange
        string? nullHash = null;
        string? nullNick = null;

        // Act
        var request = new DeleteUserInfoRequest(nullHash, nullNick);

        // Assert
        Assert.Null(request.UserNickNameHash);
        Assert.Null(request.UserNickName);
    }

    [Fact]
    public void UserNickNameHash_Property_IsInitOnly()
    {
        // Arrange & Act
        var request = new DeleteUserInfoRequest("constructor_hash", "constructor_nick");

        // Assert
        Assert.Equal("constructor_hash", request.UserNickNameHash);
        Assert.Equal("constructor_nick", request.UserNickName);
        
        var property = typeof(DeleteUserInfoRequest).GetProperty(nameof(DeleteUserInfoRequest.UserNickNameHash));
        var setMethod = property?.GetSetMethod();
        Assert.NotNull(setMethod);
        
        var property1 = typeof(DeleteUserInfoRequest).GetProperty(nameof(DeleteUserInfoRequest.UserNickName));
        var setMethod1 = property1?.GetSetMethod();
        Assert.NotNull(setMethod1);
        
        var requiredMemberAttribute = setMethod.ReturnParameter.GetRequiredCustomModifiers();
        var isInitOnlyModifier = requiredMemberAttribute.Any(t => t.Name.Contains("IsExternalInit"));
        Assert.True(isInitOnlyModifier, "UserNickNameHash must be init-only");
        
        var requiredMemberAttribute1 = setMethod1.ReturnParameter.GetRequiredCustomModifiers();
        var isInitOnlyModifier1 = requiredMemberAttribute1.Any(t => t.Name.Contains("IsExternalInit"));
        Assert.True(isInitOnlyModifier1, "UserNickName must be init-only");
    }

    [Fact]
    public void UserNickNameHash_InitAccessor_CanBeSetViaObjectInitializer()
    {
        // Arrange & Act
        var request = new DeleteUserInfoRequest("constructor_value", "constructor_nick")
        {
            UserNickNameHash = "initializer_value",
            UserNickName = "initializer_nick"
        };

        // Assert
        Assert.Equal("initializer_value", request.UserNickNameHash);
        Assert.Equal("initializer_nick", request.UserNickName);
    }

    [Fact]
    public void UserNickNameHash_InitAccessor_OverridesConstructorValue()
    {
        // Arrange
        var constructorValue = "constructor_hash";
        var constructorValueNick = "constructor_nick";
        var initializerValue = "initializer_hash";
        var initializerValueNick = "initializer_nick";

        // Act
        var request = new DeleteUserInfoRequest(constructorValue, initializerValueNick)
        {
            UserNickNameHash = initializerValue,
            UserNickName = initializerValueNick
        };

        // Assert
        Assert.Equal(initializerValue, request.UserNickNameHash);
        Assert.Equal(initializerValueNick, request.UserNickName);
        Assert.NotEqual(constructorValue, request.UserNickNameHash);
        Assert.NotEqual(constructorValueNick, request.UserNickName);
    }

    [Xunit.Theory]
    [InlineData("user123", "nickname123")]
    [InlineData("", "")]
    [InlineData("very_long_hash_string_with_special_characters_!@#$%^&*()", "very_long_nickname_string_with_special_characters_!@#$%^&*()")]
    [InlineData(null, null)]
    public void Constructor_WithVariousHashValues_CreatesValidObject(string? hash, string? nickName)
    {
        // Act
        var request = new DeleteUserInfoRequest(hash, nickName);

        // Assert
        Assert.Equal(hash, request.UserNickNameHash);
    }
}