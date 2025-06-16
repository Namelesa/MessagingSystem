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

        // Act
        var request = new DeleteUserInfoRequest(expectedHash);

        // Assert
        Assert.Equal(expectedHash, request.UserNickNameHash);
    }

    [Fact]
    public void Constructor_WithEmptyString_SetsPropertyToEmptyString()
    {
        // Arrange
        var emptyHash = string.Empty;

        // Act
        var request = new DeleteUserInfoRequest(emptyHash);

        // Assert
        Assert.Equal(emptyHash, request.UserNickNameHash);
    }

    [Fact]
    public void Constructor_WithNullValue_SetsPropertyToNull()
    {
        // Arrange
        string? nullHash = null;

        // Act
        var request = new DeleteUserInfoRequest(nullHash);

        // Assert
        Assert.Null(request.UserNickNameHash);
    }

    [Fact]
    public void UserNickNameHash_Property_IsInitOnly()
    {
        // Arrange & Act
        var request = new DeleteUserInfoRequest("constructor_hash");

        // Assert
        Assert.Equal("constructor_hash", request.UserNickNameHash);
        
        var property = typeof(DeleteUserInfoRequest).GetProperty(nameof(DeleteUserInfoRequest.UserNickNameHash));
        var setMethod = property?.GetSetMethod();
        Assert.NotNull(setMethod);
        
        var requiredMemberAttribute = setMethod.ReturnParameter.GetRequiredCustomModifiers();
        var isInitOnlyModifier = requiredMemberAttribute.Any(t => t.Name.Contains("IsExternalInit"));
        Assert.True(isInitOnlyModifier, "UserNickNameHash must be init-only");
    }

    [Fact]
    public void UserNickNameHash_InitAccessor_CanBeSetViaObjectInitializer()
    {
        // Arrange & Act
        var request = new DeleteUserInfoRequest("constructor_value")
        {
            UserNickNameHash = "initializer_value"
        };

        // Assert
        Assert.Equal("initializer_value", request.UserNickNameHash);
    }

    [Fact]
    public void UserNickNameHash_InitAccessor_OverridesConstructorValue()
    {
        // Arrange
        var constructorValue = "constructor_hash";
        var initializerValue = "initializer_hash";

        // Act
        var request = new DeleteUserInfoRequest(constructorValue)
        {
            UserNickNameHash = initializerValue
        };

        // Assert
        Assert.Equal(initializerValue, request.UserNickNameHash);
        Assert.NotEqual(constructorValue, request.UserNickNameHash);
    }

    [Xunit.Theory]
    [InlineData("user123")]
    [InlineData("")]
    [InlineData("very_long_hash_string_with_special_characters_!@#$%^&*()")]
    [InlineData(null)]
    public void Constructor_WithVariousHashValues_CreatesValidObject(string? hash)
    {
        // Act
        var request = new DeleteUserInfoRequest(hash);

        // Assert
        Assert.Equal(hash, request.UserNickNameHash);
    }
}