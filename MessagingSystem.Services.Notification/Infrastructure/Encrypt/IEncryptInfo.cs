namespace MessagingSystem.Services.Notification.Infrastructure.Encrypt;

public interface IEncryptInfo
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}