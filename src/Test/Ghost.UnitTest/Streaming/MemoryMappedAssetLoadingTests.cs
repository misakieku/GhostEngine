using Ghost.Core;
using Ghost.Engine.Streaming;
using K4os.Compression.LZ4;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;
using ZstdSharp;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public unsafe class MemoryMappedAssetLoadingTests
{
    [TestMethod]
    public void Test_PackFileMapping_OpensAndReadsMappedFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_pack_{Guid.NewGuid():N}.pack");
        try
        {
            var header = new PackFileHeader
            {
                magic = PackFileHeader.MAGIC,
                version = PackFileHeader.VERSION,
                reserved = 0
            };

            var payloadBytes = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };
            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                header.WriteTo(fs);
                fs.Write(payloadBytes);
            }

            using (var mapping = new PackFileMapping(tempFile))
            {
                Assert.AreEqual(PackFileHeader.SIZE + payloadBytes.Length, mapping.Length);
                Assert.IsTrue(mapping.BasePointer != null);

                var span = mapping.GetSpan(PackFileHeader.SIZE, payloadBytes.Length);
                Assert.AreEqual(payloadBytes.Length, span.Length);
                for (var i = 0; i < payloadBytes.Length; i++)
                {
                    Assert.AreEqual(payloadBytes[i], span[i]);
                }

                Span<byte> destination = stackalloc byte[payloadBytes.Length];
                mapping.Read(PackFileHeader.SIZE, destination);
                for (var i = 0; i < payloadBytes.Length; i++)
                {
                    Assert.AreEqual(payloadBytes[i], destination[i]);
                }
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [TestMethod]
    public void Test_AssetPayload_MappedMemory_ProvidesZeroCopyReadAndUniformDispose()
    {
        Span<byte> buffer = stackalloc byte[32];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(i + 1);
        }

        fixed (byte* pBuffer = buffer)
        {
            var payload = AssetPayload.FromMappedMemory(pBuffer, buffer.Length);
            Assert.IsTrue(payload.IsValid);
            Assert.AreEqual((long)buffer.Length, payload.Size);

            var slice = payload.Slice(4, 4);
            Assert.AreEqual(5, slice[0]);
            Assert.AreEqual(6, slice[1]);
            Assert.AreEqual(7, slice[2]);
            Assert.AreEqual(8, slice[3]);

            Span<byte> dst = stackalloc byte[4];
            payload.Read(8, dst);
            Assert.AreEqual(9, dst[0]);
            Assert.AreEqual(10, dst[1]);

            var val = payload.Read<uint>(0);
            Assert.AreEqual(MemoryMarshal.Read<uint>(buffer), val);

            payload.Dispose();
            Assert.IsFalse(payload.IsValid);
            Assert.IsTrue(payload.Pointer == null);
            Assert.AreEqual(0L, payload.Size);
        }
    }

    [TestMethod]
    public void Test_AssetPayload_AllocatedMemory_FreesMemoryOnDispose()
    {
        var block = new MemoryBlock(64, 16, AllocationHandle.Persistent);
        var pBlock = (byte*)block.GetUnsafePtr();
        pBlock[0] = 42;
        pBlock[1] = 99;

        var payload = AssetPayload.FromAllocatedMemory(block, 64);
        Assert.IsTrue(payload.IsValid);
        Assert.AreEqual(64L, payload.Size);
        Assert.AreEqual((byte)42, payload.Span[0]);
        Assert.AreEqual((byte)99, payload.Span[1]);

        payload.Dispose();
        Assert.IsFalse(payload.IsValid);
        Assert.IsTrue(payload.Pointer == null);
        Assert.AreEqual(0L, payload.Size);
    }

    [TestMethod]
    public void Test_RuntimeContentProvider_LoadsMappedAndCompressedPayloads()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pack_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var packFileName = "test_data.pack";
        var packPath = Path.Combine(tempDir, packFileName);
        var manifestPath = Path.Combine(tempDir, "manifest.json");

        var rawId = Guid.NewGuid();
        var lz4Id = Guid.NewGuid();
        var zstdId = Guid.NewGuid();

        // Use an unaligned size (333 bytes) to verify that MemoryBlock alignment requirement is properly handled
        var originalData = new byte[333];
        for (var i = 0; i < originalData.Length; i++)
        {
            originalData[i] = (byte)(i * 3 + 7);
        }

        // Compress LZ4
        var lz4Compressed = new byte[LZ4Codec.MaximumOutputSize(originalData.Length)];
        var lz4Size = LZ4Codec.Encode(originalData, lz4Compressed);

        // Compress Zstd
        using var compressor = new Compressor();
        var zstdCompressed = compressor.Wrap(originalData).ToArray();

        long rawOffset, lz4Offset, zstdOffset;
        using (var fs = new FileStream(packPath, FileMode.Create, FileAccess.Write))
        {
            var header = new PackFileHeader
            {
                magic = PackFileHeader.MAGIC,
                version = PackFileHeader.VERSION,
                reserved = 0
            };
            header.WriteTo(fs);

            rawOffset = fs.Position;
            fs.Write(originalData);

            lz4Offset = fs.Position;
            fs.Write(lz4Compressed, 0, lz4Size);

            zstdOffset = fs.Position;
            fs.Write(zstdCompressed);
        }

        var manifest = new Manifest();
        manifest.AddAsset("Assets/Raw", new AssetInfo
        {
            AssetId = rawId,
            AssetType = AssetType.Mesh,
            Compression = CompressionMethod.None,
            PackFileName = packFileName,
            Offset = rawOffset,
            Size = originalData.Length,
            UncompressedSize = originalData.Length
        });
        manifest.AddAsset("Assets/LZ4", new AssetInfo
        {
            AssetId = lz4Id,
            AssetType = AssetType.Texture,
            Compression = CompressionMethod.LZ4,
            PackFileName = packFileName,
            Offset = lz4Offset,
            Size = lz4Size,
            UncompressedSize = originalData.Length
        });
        manifest.AddAsset("Assets/Zstd", new AssetInfo
        {
            AssetId = zstdId,
            AssetType = AssetType.Texture,
            Compression = CompressionMethod.Zstd,
            PackFileName = packFileName,
            Offset = zstdOffset,
            Size = zstdCompressed.Length,
            UncompressedSize = originalData.Length
        });

        manifest.SaveToDiskAsync(manifestPath).GetAwaiter().GetResult();

        try
        {
            using var provider = new RuntimeContentProvider(manifestPath);

            // 1. Test uncompressed MMF
            var rawResult = provider.OpenReadAsync(rawId);
            Assert.IsTrue(rawResult.IsSuccess);
            using (var rawPayload = rawResult.Value)
            {
                Assert.IsTrue(rawPayload.IsValid);
                Assert.AreEqual(originalData.Length, rawPayload.Size);
                CollectionAssert.AreEqual(originalData, rawPayload.Span.ToArray());
            }

            // 2. Test LZ4 decompression
            var lz4Result = provider.OpenReadAsync(lz4Id);
            Assert.IsTrue(lz4Result.IsSuccess);
            using (var lz4Payload = lz4Result.Value)
            {
                Assert.IsTrue(lz4Payload.IsValid);
                Assert.AreEqual(originalData.Length, lz4Payload.Size);
                CollectionAssert.AreEqual(originalData, lz4Payload.Span.ToArray());
            }

            // 3. Test Zstd decompression
            var zstdResult = provider.OpenReadAsync(zstdId);
            Assert.IsTrue(zstdResult.IsSuccess);
            using (var zstdPayload = zstdResult.Value)
            {
                Assert.IsTrue(zstdPayload.IsValid);
                Assert.AreEqual(originalData.Length, zstdPayload.Size);
                CollectionAssert.AreEqual(originalData, zstdPayload.Span.ToArray());
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
