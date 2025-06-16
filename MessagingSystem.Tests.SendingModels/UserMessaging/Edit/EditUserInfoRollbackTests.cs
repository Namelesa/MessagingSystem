using MessagingSystem.SendingModels.UserMessaging.Edit;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.Edit;

public class EditUserInfoRollbackTests
{
    [Fact]
    public void Constructor_WithTrueValue_SetsIsSuccessToTrue()
    {
        // Arrange
        const bool expectedIsSuccess = true;

        // Act
        var rollback = new EditUserRollBack(expectedIsSuccess);

        // Assert
        Assert.True(rollback.IsSuccess);
    }

    [Fact]
    public void Constructor_WithFalseValue_SetsIsSuccessToFalse()
    {
        // Arrange
        const bool expectedIsSuccess = false;

        // Act
        var rollback = new EditUserRollBack(expectedIsSuccess);

        // Assert
        Assert.False(rollback.IsSuccess);
    }

    [Fact]
    public void IsSuccess_Property_CanBeModified()
    {
        // Arrange
        const bool initialValue = false;
        const bool newValue = true;
        var rollback = new EditUserRollBack(initialValue)
        {
            // Act
            IsSuccess = newValue
        };

        // Assert
        Assert.Equal(newValue, rollback.IsSuccess);
    }

    [Xunit.Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_WithBooleanValue_SetsCorrectly(bool isSuccess)
    {
        // Act
        var rollback = new EditUserRollBack(isSuccess);

        // Assert
        Assert.Equal(isSuccess, rollback.IsSuccess);
    }

    [Fact]
    private void IsSuccess_CanBeToggledMultipleTimes()
    {
        // Arrange
        var rollback = new EditUserRollBack(false)
        {
            // Act & Assert
            IsSuccess = true
        };

        Assert.True(rollback.IsSuccess);

        // Act & Assert
        rollback.IsSuccess = false;
        Assert.False(rollback.IsSuccess);

        // Act & Assert 
        rollback.IsSuccess = true;
        Assert.True(rollback.IsSuccess);
    }

    [Fact]
    public void ObjectInitializer_OverridesConstructorValue()
    {
        // Arrange
        const bool constructorValue = false;
        const bool initializerValue = true;

        // Act
        var rollback = new EditUserRollBack(constructorValue)
        {
            IsSuccess = initializerValue
        };

        // Assert
        Assert.Equal(initializerValue, rollback.IsSuccess);
        Assert.NotEqual(constructorValue, rollback.IsSuccess);
    }
}