namespace Encryptor.Encryption;

public interface IEncryptionInfo
{
    string Encrypt(string plainText);
    string EncryptRsa(string plainText, string baseKey);
    void EncryptRsaObjectStrings<T>(T obj, string baseKey);
    void EncryptObjectStringsForUpdate<T>(T obj);
    public void EncryptObjectStrings<T>(T obj);
    public string GetPublicKey();
}