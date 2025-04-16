namespace MessagingSystem.Services.User.Infrastructure.Encrypt;

public interface IEncryptInfo
{
    string Encrypt(string plainText);
    string EncryptRsa(string plainText, string baseKey);
    string Decrypt(string cipherText);
    string DecryptRsa(string baseKey);
    string GetPublicKey();
}