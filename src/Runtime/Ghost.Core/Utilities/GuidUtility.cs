using System;
using System.IO.Hashing;
using System.Text;

namespace Ghost.Core.Utilities;

public static class GuidUtility
{
    public static Guid DeriveSubAssetGuid(Guid parent, string subPath)
    {
        Span<byte> input = stackalloc byte[16 + Encoding.UTF8.GetByteCount(subPath)];
        parent.TryWriteBytes(input);
        Encoding.UTF8.GetBytes(subPath, input[16..]);
        var hash = XxHash128.HashToUInt128(input);
        
        // Guid constructor expects bytes in a specific order (little-endian for first 3 parts, big-endian for the rest)
        // UInt128 bytes are just bytes. Let's just create a byte array and pass to Guid constructor.
        Span<byte> hashBytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(hashBytes, hash);
        return new Guid(hashBytes);
    }
}
