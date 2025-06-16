using MessagingSystem.SendingModels.UserMessaging.IsExist.Users;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.SendingModels.UserMessaging.IsExist.Users;

public class ExistingUsersRequestTests
{
    [Fact]
    public void Constructor_WithValidNickNamesList_SetsPropertyCorrectly()
    {
        // Arrange
        var expectedNickNames = new List<string> { "user1", "user2", "user3" };

        // Act
        var request = new ExistingUsersRequest(expectedNickNames);

        // Assert
        Assert.Equal(expectedNickNames, request.NickNames);
        Assert.Equal(3, request.NickNames.Count);
    }

    [Fact]
    public void Constructor_WithEmptyList_SetsPropertyToEmptyList()
    {
        // Arrange
        var emptyList = new List<string>();

        // Act
        var request = new ExistingUsersRequest(emptyList);

        // Assert
        Assert.Equal(emptyList, request.NickNames);
        Assert.Empty(request.NickNames);
    }

    [Fact]
    public void Constructor_WithNullValue_SetsPropertyToNull()
    {
        // Arrange
        List<string>? nullList = null;

        // Act
        var request = new ExistingUsersRequest(nullList);

        // Assert
        Assert.Null(request.NickNames);
    }

    [Fact]
    public void Constructor_WithListContainingNullValues_PreservesNullValues()
    {
        // Arrange
        var listWithNulls = new List<string> { "user1", null, "user3", null };

        // Act
        var request = new ExistingUsersRequest(listWithNulls);

        // Assert
        Assert.Equal(listWithNulls, request.NickNames);
        Assert.Equal(4, request.NickNames.Count);
        Assert.Null(request.NickNames[1]);
        Assert.Null(request.NickNames[3]);
    }

    [Fact]
    public void Constructor_WithListContainingEmptyStrings_PreservesEmptyStrings()
    {
        // Arrange
        var listWithEmptyStrings = new List<string> { "user1", "", "user3" };

        // Act
        var request = new ExistingUsersRequest(listWithEmptyStrings);

        // Assert
        Assert.Equal(listWithEmptyStrings, request.NickNames);
        Assert.Equal(string.Empty, request.NickNames[1]);
    }

    [Fact]
    public void NickNames_InitAccessor_CanBeSetViaObjectInitializer()
    {
        // Arrange
        var constructorList = new List<string> { "constructor1", "constructor2" };
        var initializerList = new List<string> { "initializer1", "initializer2", "initializer3" };

        // Act
        var request = new ExistingUsersRequest(constructorList)
        {
            NickNames = initializerList
        };

        // Assert
        Assert.Equal(initializerList, request.NickNames);
        Assert.Equal(3, request.NickNames.Count);
        Assert.NotEqual(constructorList, request.NickNames);
    }

    [Fact]
    public void NickNames_InitAccessor_CanBeSetToNull()
    {
        // Arrange
        var constructorList = new List<string> { "user1", "user2" };

        // Act
        var request = new ExistingUsersRequest(constructorList)
        {
            NickNames = null
        };

        // Assert
        Assert.Null(request.NickNames);
    }

    [Xunit.Theory]
    [InlineData(new object[] { "user1" })]
    [InlineData(new object[] { "user1", "user2", "user3" })]
    [InlineData(new object[] { })]
    public void Constructor_WithVariousListSizes_CreatesValidObject(params string[] nickNames)
    {
        // Arrange
        var nickNamesList = nickNames.ToList();

        // Act
        var request = new ExistingUsersRequest(nickNamesList);

        // Assert
        Assert.Equal(nickNamesList, request.NickNames);
        Assert.Equal(nickNames.Length, request.NickNames.Count);
    }
}