using Encryptor.Decryption;
using Encryptor.Encryption;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;
using MessagingSystem.Services.Messaging.Core.Groups.Group;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupInformation;

public class GroupEncryptionDecoratorTests
{
    private readonly Mock<IEncryptionInfo> _encryptionMock;
    private readonly Mock<IDecryptionInfo> _decryptionMock;
    private readonly GroupEncryptionDecorator _decorator;

    public GroupEncryptionDecoratorTests()
    {
        _encryptionMock = new Mock<IEncryptionInfo>();
        _decryptionMock = new Mock<IDecryptionInfo>();
        _decorator = new GroupEncryptionDecorator(_encryptionMock.Object, _decryptionMock.Object);
    }

    [Fact]
    public void Encrypt_ShouldEncryptGroupAndMembers()
    {
        // Arrange
        var group = new GroupInfo("TestGroup", "TestImage", "TestDescription", "TestOwner");
        group.AddUsers(["Alice", "Bob"]);

        _encryptionMock.Setup(e => e.EncryptObjectStrings(group));
        _encryptionMock.Setup(e => e.Encrypt("Alice")).Returns("Enc_Alice");
        _encryptionMock.Setup(e => e.Encrypt("Bob")).Returns("Enc_Bob");

        // Act
        _decorator.Encrypt(group);

        // Assert
        _encryptionMock.Verify(e => e.EncryptObjectStrings(group), Times.Once);
        Assert.Equal(
            ["Enc_Alice", "Enc_Bob"],
            group.Members.Select(m => m.UserNickName).ToList()
        );
    }
    
    [Fact]
    public void Decrypt_ShouldDecryptGroupAndMembers()
    {
        // Arrange
        var group = new GroupInfo("TestGroup", "TestImage", "TestDescription", "TestOwner");
        group.AddUsers(["Enc_Alice", "Enc_Bob"]);
        _decryptionMock.Setup(d => d.DecryptObjectStrings(group));
        _decryptionMock.Setup(d => d.Decrypt("Enc_Alice")).Returns("Alice");
        _decryptionMock.Setup(d => d.Decrypt("Enc_Bob")).Returns("Bob");

        // Act
        _decorator.Decrypt(group);

        // Assert
        _decryptionMock.Verify(d => d.DecryptObjectStrings(group), Times.Once);
        Assert.Equal(
            ["Alice", "Bob"],
            group.Members.Select(m => m.UserNickName).ToList()
        );
    }


    [Fact]
    public void EncryptMembers_ShouldReturnEncryptedValue()
    {
        // Arrange
        var plainText = "test";
        var encrypted = "encrypted";
        _encryptionMock.Setup(e => e.Encrypt(plainText)).Returns(encrypted);

        // Act
        var result = _decorator.EncryptMembers(plainText);

        // Assert
        Assert.Equal(encrypted, result);
    }

    [Fact]
    public void DecryptMembers_ShouldReturnDecryptedValue()
    {
        // Arrange
        var encrypted = "encrypted";
        var decrypted = "test";
        _decryptionMock.Setup(d => d.Decrypt(encrypted)).Returns(decrypted);

        // Act
        var result = _decorator.DecryptMembers(encrypted);

        // Assert
        Assert.Equal(decrypted, result);
    }

    [Fact]
    public void DecryptGeneric_ShouldCallDecryptObjectStrings()
    {
        // Arrange
        var testObject = new DummyObject();
        _decryptionMock.Setup(d => d.DecryptObjectStrings(testObject));

        // Act
        _decorator.DecryptGeneric(testObject);

        // Assert
        _decryptionMock.Verify(d => d.DecryptObjectStrings(testObject), Times.Once);
    }

    private class DummyObject { }
}
