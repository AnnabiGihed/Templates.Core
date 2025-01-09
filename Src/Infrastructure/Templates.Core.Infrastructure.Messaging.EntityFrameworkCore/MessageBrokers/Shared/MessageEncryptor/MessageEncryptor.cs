using System.Text;
using System.Security.Cryptography;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageEncryptor;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageEncryptor;

public class AesMessageEncryptor : IMessageEncryptor
{
	#region Properties
	protected readonly byte[] _encryptionKey;
	#endregion

	#region Constructor
	public AesMessageEncryptor(string encryptionKey)
	{
		if (string.IsNullOrEmpty(encryptionKey))
			throw new ArgumentNullException(nameof(encryptionKey));

		_encryptionKey = Encoding.UTF8.GetBytes(encryptionKey);

		if (_encryptionKey.Length != 32)
		{
			throw new ArgumentException("EncryptionKey must be 32 characters long for AES-256 encryption.");
		}
	}
	#endregion

	#region IMessageEncryptor Implementation
	public byte[] Encrypt(byte[] data)
	{
		if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));

		using var aes = Aes.Create();
		aes.Key = _encryptionKey;
		aes.GenerateIV();

		using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
		using var memoryStream = new MemoryStream();
		memoryStream.Write(aes.IV, 0, aes.IV.Length);
		using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
		{
			cryptoStream.Write(data, 0, data.Length);
		}
		return memoryStream.ToArray();
	}
	public byte[] Decrypt(byte[] encryptedData)
	{
		using var aes = Aes.Create();
		aes.Key = _encryptionKey;

		using var memoryStream = new MemoryStream(encryptedData);
		var iv = new byte[aes.BlockSize / 8];
		memoryStream.Read(iv, 0, iv.Length);
		aes.IV = iv;

		using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
		using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
		using var resultStream = new MemoryStream();
		cryptoStream.CopyTo(resultStream);
		return resultStream.ToArray();
	}
	#endregion
}