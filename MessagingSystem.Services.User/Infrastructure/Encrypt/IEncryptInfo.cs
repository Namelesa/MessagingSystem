namespace MessagingSystem.Services.User.Infrastructure.Encrypt;

public interface IEncryptInfo
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}