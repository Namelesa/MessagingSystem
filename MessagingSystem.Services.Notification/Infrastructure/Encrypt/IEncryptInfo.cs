namespace MessagingSystem.Services.Notification.Infrastructure.Encrypt;

public interface IEncryptInfo
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    void DecryptObjectStrings<T>(T obj);
    void DecryptRsaObjectStrings<T>(T obj);
    string GetPublicKey();
}