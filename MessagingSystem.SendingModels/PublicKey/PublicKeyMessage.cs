namespace MessagingSystem.SendingModels.PublicKey;

public class PublicKeyMessage
{ 
    public string ServiceName { get; set; }
    public required string PublicKey { get; set; }
}