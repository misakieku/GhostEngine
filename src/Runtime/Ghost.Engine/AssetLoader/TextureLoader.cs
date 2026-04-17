using Ghost.Core;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Engine.AssetLoader;

internal sealed class TextureLoader : IRuntimeAssetLoader
{
    public static readonly AssetType AssetType = AssetType.Texture;

    public async ValueTask<Result<Asset>> LoadAsync(Stream cookedData, Guid id, CancellationToken token)
    {
        var header = new TextureContentHeader();
        cookedData.ReadExactly(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref header)));

        var alignment = header.depth switch
        {
            8 => MemoryUtility.AlignOf<byte>(),
            16 => MemoryUtility.AlignOf<ushort>(),
            32 => MemoryUtility.AlignOf<float>(),
            _ => MemoryUtility.AlignOf<float>()
        };

        var data = new MemoryBlock((nuint)(cookedData.Length - cookedData.Position), alignment, AllocationHandle.Persistent);

        // C# built-in collections use int for indexing, so we need to ensure that the buffer size does not exceed int.MaxValue
        var maxBufferSize = (int)Math.Min(0x7effffffu, header.width * header.height * header.depth / 8u * header.colorComponents);
        var offset = 0u;

        while (offset < data.Size)
        {
            using var memoryManager = NativeMemoryManager<byte>.FromMemoryBlock(data, (int)offset, maxBufferSize);

            await cookedData.ReadExactlyAsync(memoryManager.Memory, token);
            offset += (uint)memoryManager.Memory.Length;
        }

        return new TextureAsset(ref data, header, id);
    }
}