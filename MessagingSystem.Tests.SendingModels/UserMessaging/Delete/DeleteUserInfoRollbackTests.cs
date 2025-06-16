using MessagingSystem.SendingModels.UserMessaging.Delete;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.Delete;

public class DeleteUserInfoRollbackTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidUserNickNameHash_SetsPropertiesCorrectly()
    {
        // Arrange
        const string expectedHash = "abc123hash";

        // Act
        var rollback = new DeleteUserInfoRollback(expectedHash);

        // Assert
        Assert.Equal(expectedHash, rollback.UserNickNameHash);
        Assert.False(rollback.IsSuccess);
    }

    [Fact]
    public void Constructor_WithEmptyString_SetsPropertyToEmptyString()
    {
        // Arrange
        var emptyHash = string.Empty;

        // Act
        var rollback = new DeleteUserInfoRollback(emptyHash);

        // Assert
        Assert.Equal(string.Empty, rollback.UserNickNameHash);
        Assert.False(rollback.IsSuccess);
    }

    [Fact]
    public void Constructor_WithNullValue_SetsPropertyToNull()
    {
        // Arrange
        string? nullHash = null;

        // Act
        var rollback = new DeleteUserInfoRollback(nullHash);

        // Assert
        Assert.Null(rollback.UserNickNameHash);
        Assert.False(rollback.IsSuccess);
    }

    [Xunit.Theory]
    [InlineData("user123")]
    [InlineData("")]
    [InlineData("very_long_hash_string_with_special_characters_@#$%")]
    [InlineData("русский_хеш")]
    public void Constructor_WithVariousHashValues_SetsUserNickNameHashCorrectly(string hash)
    {
        // Act
        var rollback = new DeleteUserInfoRollback(hash);

        // Assert
        Assert.Equal(hash, rollback.UserNickNameHash);
        Assert.False(rollback.IsSuccess);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void UserNickNameHash_CanBeModified()
    {
        // Arrange
        const string initialHash = "initial_hash";
        const string newHash = "new_hash";
        var rollback = new DeleteUserInfoRollback(initialHash)
        {
            // Act
            UserNickNameHash = newHash
        };

        // Assert
        Assert.Equal(newHash, rollback.UserNickNameHash);
    }

    [Fact]
    public void UserNickNameHash_CanBeSetToNull()
    {
        // Arrange
        var rollback = new DeleteUserInfoRollback("initial_hash")
        {
            // Act
            UserNickNameHash = null
        };

        // Assert
        Assert.Null(rollback.UserNickNameHash);
    }

    [Fact]
    public void UserNickNameHash_CanBeSetToEmptyString()
    {
        // Arrange
        var rollback = new DeleteUserInfoRollback("initial_hash")
        {
            // Act
            UserNickNameHash = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, rollback.UserNickNameHash);
    }

    [Fact]
    public void IsSuccess_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var rollback = new DeleteUserInfoRollback("test_hash");

        // Assert
        Assert.False(rollback.IsSuccess);
    }

    [Fact]
    public void IsSuccess_CanBeSetToTrue()
    {
        // Arrange
        var rollback = new DeleteUserInfoRollback("test_hash")
        {
            // Act
            IsSuccess = true
        };

        // Assert
        Assert.True(rollback.IsSuccess);
    }

    [Fact]
    public void IsSuccess_CanBeSetToFalse()
    {
        // Arrange
        var rollback = new DeleteUserInfoRollback("test_hash") { IsSuccess = true };

        // Act
        rollback.IsSuccess = false;

        // Assert
        Assert.False(rollback.IsSuccess);
    }

    #endregion

    #region Object Initializer Tests

    [Fact]
    public void ObjectInitializer_WithIsSuccessTrue_SetsCorrectly()
    {
        // Arrange
        const string hash = "test_hash";

        // Act
        var rollback = new DeleteUserInfoRollback(hash) { IsSuccess = true };

        // Assert
        Assert.Equal(hash, rollback.UserNickNameHash);
        Assert.True(rollback.IsSuccess);
    }

    [Xunit.Theory]
    [InlineData("user123", true)]
    [InlineData("user456", false)]
    [InlineData("", true)]
    [InlineData("", false)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    public void ObjectInitializer_WithVariousParameters_CreatesValidObject(string hash, bool isSuccess)
    {
        // Act
        var rollback = new DeleteUserInfoRollback(hash) { IsSuccess = isSuccess };

        // Assert
        Assert.Equal(hash, rollback.UserNickNameHash);
        Assert.Equal(isSuccess, rollback.IsSuccess);
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public void Properties_AreIndependentlyModifiable()
    {
        // Arrange
        var rollback = new DeleteUserInfoRollback("initial_hash");
        const string newHash = "modified_hash";

        // Act
        rollback.UserNickNameHash = newHash;
        rollback.IsSuccess = true;

        // Assert
        Assert.Equal(newHash, rollback.UserNickNameHash);
        Assert.True(rollback.IsSuccess);
    }

    [Fact]
    public void MultipleModifications_MaintainState()
    {
        // Arrange
        var rollback = new DeleteUserInfoRollback("hash1")
        {
            // Act & Assert - Multiple state changes
            UserNickNameHash = "hash2",
            IsSuccess = true
        };

        Assert.Equal("hash2", rollback.UserNickNameHash);
        Assert.True(rollback.IsSuccess);

        rollback.UserNickNameHash = "hash3";
        rollback.IsSuccess = false;
        Assert.Equal("hash3", rollback.UserNickNameHash);
        Assert.False(rollback.IsSuccess);

        rollback.UserNickNameHash = null;
        Assert.Null(rollback.UserNickNameHash);
        Assert.False(rollback.IsSuccess);
    }

    #endregion
}