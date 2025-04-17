namespace MessagingSystem.Services.User.Infrastructure.Encrypt;

public interface IEncryptInfo
{
    string Encrypt(string plainText);
    string EncryptRsa(string plainText, string baseKey);
    void EncryptRsaObjectStrings<T>(T obj, string baseKey);
    void EncryptObjectStringsForUpdate<T>(T obj);
    public void EncryptObjectStrings<T>(T obj);
    string Decrypt(string cipherText);
    string DecryptRsa(string baseKey);
    public void DecryptRsaObjectStrings<T>(T obj);
    public void DecryptObjectStrings<T>(T obj);
    string GetPublicKey();
}