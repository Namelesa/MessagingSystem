using MessagingSystem.Services.Messaging.Core.Oto.Users;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Core.Oto;

public class UserImageTests
{
    [Fact]
    public void Constructor_SetsAllRequiredProperties()
    {
        // Arrange
        const string nickNameHash = "hashedNickName";
        const string image = "base64ImageData";
        
        // Act
        var userImage = new UserImage(nickNameHash, image);
        
        // Assert
        Assert.Equal(nickNameHash, userImage.NickNameHash);
        Assert.Equal(image, userImage.Image);
    }
    
    [Fact]
    public void Id_Is_Empty_By_Default()
    {
        // Arrange & Act
        var userImage = new UserImage("hash", "image");

        // Assert
        Assert.Equal(Guid.Empty, userImage.Id);
    }
    
    [Fact]
    public void Id_Can_Be_Set_Through_Init()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        
        // Act
        var userImage = new UserImage("hash", "image") { Id = expectedId };
        
        // Assert
        Assert.Equal(expectedId, userImage.Id);
    }
    
    [Fact]
    public void NickNameHash_Property_Returns_Constructor_Value()
    {
        // Arrange
        const string expectedHash = "testHash123";
        
        // Act
        var userImage = new UserImage(expectedHash, "image");
        
        // Assert - Multiple accesses to ensure getter coverage
        Assert.Equal(expectedHash, userImage.NickNameHash);
        var hashFromProperty = userImage.NickNameHash;
        Assert.Equal(expectedHash, hashFromProperty);
        Assert.NotNull(userImage.NickNameHash);
    }
    
    [Fact]
    public void Image_Property_Returns_Constructor_Value()
    {
        // Arrange
        const string expectedImage = "testImageData";
        
        // Act
        var userImage = new UserImage("hash", expectedImage);
        
        // Assert - Multiple accesses to ensure getter coverage
        Assert.Equal(expectedImage, userImage.Image);
        var imageFromProperty = userImage.Image;
        Assert.Equal(expectedImage, imageFromProperty);
        Assert.NotNull(userImage.Image);
    }
    
    [Fact]
    public void EditInfo_Updates_NickNameHash_And_Image()
    {
        // Arrange
        var userImage = new UserImage("originalHash", "originalImage");
        const string newHash = "newHash123";
        const string newImage = "newImageData";

        // Act
        userImage.EditInfo(newHash, newImage);

        // Assert
        Assert.Equal(newHash, userImage.NickNameHash);
        Assert.Equal(newImage, userImage.Image);
    }
    
    [Fact]
    public void EditInfo_With_Empty_Strings_Updates_Properties()
    {
        // Arrange
        var userImage = new UserImage("originalHash", "originalImage");

        // Act
        userImage.EditInfo("", "");

        // Assert
        Assert.Equal("", userImage.NickNameHash);
        Assert.Equal("", userImage.Image);
    }
    
    [Fact]
    public void EditInfo_With_Null_Values_Updates_Properties()
    {
        // Arrange
        var userImage = new UserImage("originalHash", "originalImage");

        // Act
        userImage.EditInfo(null, null);

        // Assert
        Assert.Null(userImage.NickNameHash);
        Assert.Null(userImage.Image);
    }
    
    [Xunit.Theory]
    [InlineData("hash1", "image1")]
    [InlineData("", "")]
    [InlineData("specialHash!@#", "longImageDataString")]
    [InlineData(null, null)]
    public void Constructor_WithDifferentParameters_SetsPropertiesCorrectly(string nickNameHash, string image)
    {
        // Act
        var userImage = new UserImage(nickNameHash, image);
        
        // Assert
        Assert.Equal(nickNameHash, userImage.NickNameHash);
        Assert.Equal(image, userImage.Image);
        
        // Ensure both getter and setter are covered through multiple access
        var hashValue = userImage.NickNameHash;
        var imageValue = userImage.Image;
        
        Assert.Equal(nickNameHash, hashValue);
        Assert.Equal(image, imageValue);
    }
    
    [Fact]
    public void NickNameHash_Getter_Coverage_Test()
    {
        // Arrange
        var userImage1 = new UserImage("hash1", "image");
        var userImage2 = new UserImage(null, "image");
        var userImage3 = new UserImage("", "image");
        
        // Act & Assert - Multiple scenarios to ensure getter coverage
        Assert.Equal("hash1", userImage1.NickNameHash);
        Assert.Null(userImage2.NickNameHash);
        Assert.Equal("", userImage3.NickNameHash);
    }
    
    [Fact]
    public void Image_Getter_Coverage_Test()
    {
        // Arrange
        var userImage1 = new UserImage("hash", "image1");
        var userImage2 = new UserImage("hash", null);
        var userImage3 = new UserImage("hash", "");
        
        // Act & Assert
        Assert.Equal("image1", userImage1.Image);
        Assert.Null(userImage2.Image);
        Assert.Equal("", userImage3.Image);
    }
    
    [Fact]
    public void Multiple_EditInfo_Calls_Work_Correctly()
    {
        // Arrange
        var userImage = new UserImage("originalHash", "originalImage");

        // Act
        userImage.EditInfo("firstEdit", "firstImage");
        userImage.EditInfo("secondEdit", "secondImage");

        // Assert
        Assert.Equal("secondEdit", userImage.NickNameHash);
        Assert.Equal("secondImage", userImage.Image);
    }
    
    [Fact]
    public void NickNameHash_PrivateSetter_Coverage_Through_Reflection()
    {
        // Arrange
        var userImage = new UserImage("originalHash", "image");
        var nickNameHashProperty = typeof(UserImage).GetProperty("NickNameHash");
        
        // Act
        nickNameHashProperty?.SetValue(userImage, "ReflectionHash");
        
        // Assert
        Assert.Equal("ReflectionHash", userImage.NickNameHash);
    }
    
    [Fact]
    public void Image_PrivateSetter_Coverage_Through_Reflection()
    {
        // Arrange
        var userImage = new UserImage("hash", "originalImage");
        var imageProperty = typeof(UserImage).GetProperty("Image");
        
        // Act
        imageProperty?.SetValue(userImage, "ReflectionImage");
        
        // Assert
        Assert.Equal("ReflectionImage", userImage.Image);
    }
    
    [Fact]
    public void EditInfo_And_Constructor_Integration_Test()
    {
        // Arrange
        const string initialHash = "initialHash";
        const string initialImage = "initialImage";
        const string updatedHash = "updatedHash";
        const string updatedImage = "updatedImage";
        
        // Act
        var userImage = new UserImage(initialHash, initialImage);
        
        // Verify initial state
        Assert.Equal(initialHash, userImage.NickNameHash);
        Assert.Equal(initialImage, userImage.Image);
        
        // Update and verify
        userImage.EditInfo(updatedHash, updatedImage);
        
        // Assert
        Assert.Equal(updatedHash, userImage.NickNameHash);
        Assert.Equal(updatedImage, userImage.Image);
    }
}