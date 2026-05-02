using Ghost.Core;
using Ghost.Engine;
using System.Collections.Concurrent;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingContentProvider : IContentProvider
{
    public class MockAssetData
    {
        public AssetType type;
        public byte[] data = Array.Empty<byte>();
        public Guid[] dependencies = Array.Empty<Guid>();

        // This is crucial for multi-threaded testing: we can inject random or fixed
        // delays to ensure our locking and state machines actually get stressed.
        public int readDelayMs = 0;
    }

    private readonly ConcurrentDictionary<Guid, MockAssetData> _assets = new();

    public void AddMockAsset(Guid guid, MockAssetData data)
    {
        _assets[guid] = data;
    }

    /// <summary>
    /// Helper method to create a valid dummy texture byte stream that the AssetEntry can parse.
    /// </summary>
    public unsafe void AddMockTexture(Guid guid, uint width = 4, uint height = 4, int readDelayMs = 0)
    {
        var header = new TextureContentHeader
        {
            width = width,
            height = height,
            bpc = 8,
            mipLevels = 1,
            dimension = 2, // Texture2D
            colorComponents = 4
        };

        // Header size is strictly 64 bytes due to [StructLayout(LayoutKind.Sequential, Size = 64)]
        var headerSize = 64;
        var pixelDataSize = width * height * 4;

        var buffer = new byte[headerSize + pixelDataSize];

        fixed (byte* pBuffer = buffer)
        {
            *(TextureContentHeader*)pBuffer = header;
            // The rest of the array remains 0 (black/transparent pixels) which is fine for tests
        }

        AddMockAsset(guid, new MockAssetData
        {
            type = AssetType.Texture,
            data = buffer,
            readDelayMs = readDelayMs
        });
    }

    public AssetType GetAssetType(Guid guid)
    {
        return _assets.TryGetValue(guid, out var asset) ? asset.type : AssetType.Unknown;
    }

    public Guid[] GetDependencies(Guid guid)
    {
        return _assets.TryGetValue(guid, out var asset) ? asset.dependencies : Array.Empty<Guid>();
    }

    public bool HasAsset(Guid guid)
    {
        return _assets.ContainsKey(guid);
    }

    public Result<Stream> OpenRead(Guid guid, CancellationToken token = default)
    {
        if (_assets.TryGetValue(guid, out var asset))
        {
            if (asset.readDelayMs > 0)
            {
                // Inject our simulated I/O latency to widen race condition windows.
                // In a real multi-threaded test, this forces the executing thread to yield 
                // and lets other threads interact with the AssetManager in the meantime.
                Thread.Sleep(asset.readDelayMs);
            }

            // Return a fast, in-memory stream representing our file
            return Result<Stream>.Success(new MemoryStream(asset.data, writable: false));
        }

        return Result<Stream>.Failure($"Mock asset {guid} not found.");
    }
}
