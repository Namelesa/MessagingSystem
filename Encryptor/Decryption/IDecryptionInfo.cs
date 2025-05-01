namespace Encryptor.Decryption;

public interface IDecryptionInfo
{
    string Decrypt(string cipherText);
    void DecryptObjectStrings<T>(T obj);
    void DecryptRsaObjectStrings<T>(T obj);
    string DecryptRsa(string baseKey);
}