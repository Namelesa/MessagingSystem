using MessagingSystem.Services.Messaging.Core.Groups.Group;

namespace MessagingSystem.Services.Messaging.Application.Group.GroupsInformation.Decorator;

public interface IGroupEncryption
{
    void Encrypt(GroupInfo group);
    void Decrypt(GroupInfo group);
    string DecryptMembers(string plainText);
    string EncryptMembers(string plainText);
    void DecryptGeneric<T>(T t);
}