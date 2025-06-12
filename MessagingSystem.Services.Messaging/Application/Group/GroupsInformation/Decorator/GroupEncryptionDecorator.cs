using Encryptor.Decryption;
using Encryptor.Encryption;
using MessagingSystem.Services.Messaging.Core.Groups.Group;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;

public class GroupEncryptionDecorator(
    IEncryptionInfo encryption, 
    IDecryptionInfo decryption
    ) : IGroupEncryption
{
    public void Encrypt(GroupInfo group)
    {
        encryption.EncryptObjectStrings(group);
        group.EncryptMembers(encryption.Encrypt);
    }
    public void Decrypt(GroupInfo group)
    {
        decryption.DecryptObjectStrings(group);
        group.EncryptMembers(decryption.Decrypt);
    }
    public string EncryptMembers(string plainText)
    {
        return encryption.Encrypt(plainText);
    }
    public string DecryptMembers(string plainText)
    {
        return decryption.Decrypt(plainText);
    }
    public void DecryptGeneric<T>(T t)
    {
        decryption.DecryptObjectStrings(t);
    }
}