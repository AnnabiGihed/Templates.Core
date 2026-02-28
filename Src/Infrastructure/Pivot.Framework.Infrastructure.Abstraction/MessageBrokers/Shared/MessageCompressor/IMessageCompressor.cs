namespace Pivot.Framework.Infrastructure.Abstraction.MessageBrokers.Shared.MessageCompressor;

public interface IMessageCompressor
{
	byte[] Compress(byte[] dataToCompress);
	public byte[] Decompress(byte[] compressedData);
}
