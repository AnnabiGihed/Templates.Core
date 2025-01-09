using System.IO.Compression;
using Templates.Core.Infrastructure.Abstraction.MessageBrokers.Shared.MessageCompressor;

namespace Templates.Core.Infrastructure.Messaging.EntityFrameworkCore.MessageBrokers.Shared.MessageCompressor;

public class GZipMessageCompressor : IMessageCompressor
{
	#region IMessageCompressor Implementation
	public byte[] Compress(byte[] dataToCompress)
	{
		if (dataToCompress == null || dataToCompress.Length == 0) throw new ArgumentNullException(nameof(dataToCompress));

		using var outputStream = new MemoryStream();
		using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
		{
			compressionStream.Write(dataToCompress, 0, dataToCompress.Length);
		}
		return outputStream.ToArray();
	}
	public byte[] Decompress(byte[] compressedData)
	{
		using var inputStream = new MemoryStream(compressedData);
		using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
		using var resultStream = new MemoryStream();
		decompressionStream.CopyTo(resultStream);
		return resultStream.ToArray();
	}
	#endregion
}