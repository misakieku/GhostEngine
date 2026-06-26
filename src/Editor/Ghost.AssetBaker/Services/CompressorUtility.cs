using Ghost.Core;
using K4os.Compression.LZ4.Streams;
using ZstdSharp;

namespace Ghost.AssetBaker.Services;

public static class CompressorUtility
{
    /// <summary>
    /// Returns a stream that compresses data written to it using the specified method and writes it to the output stream.
    /// Note: The returned stream must be disposed/closed to flush the compression buffers.
    /// </summary>
    public static Stream GetCompressionStream(Stream output, CompressionMethod method)
    {
        switch (method)
        {
            case CompressionMethod.None:
                // Return the output stream directly.
                return output;

            case CompressionMethod.Zstd:
                return new CompressionStream(output, leaveOpen: true);

            case CompressionMethod.LZ4:
                return LZ4Stream.Encode(output, leaveOpen: true);

            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }
    }
}
