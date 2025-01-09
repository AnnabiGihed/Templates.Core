namespace Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageEncryptor;

public interface IMessageEncryptor
{
	byte[] Encrypt(byte[] data);
	byte[] Decrypt(byte[] encryptedData);
}