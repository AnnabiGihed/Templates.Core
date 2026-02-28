namespace Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageEncryptor;

public interface IMessageEncryptor
{
	byte[] Encrypt(byte[] data);
	byte[] Decrypt(byte[] encryptedData);
}