using Ghost.Editor.Core.AssetHandler;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Utilities;

public static class AssetHandlerUtility
{
    public static async ValueTask SerializeAssetAsync<TSetting>(Stream stream, Guid id, Guid typeID, int handlerVersion, ReadOnlyMemory<Guid> dependencies, IAssetSettings? settings, ReadOnlyMemory<byte> contents, CancellationToken token = default)
        where TSetting : IAssetSettings
    {
        var header = new AssetMetadata(id, TextureAsset.s_typeGuid)
        {
            HandlerVersion = handlerVersion,
            DependenciesOffset = AssetMetadata.SIZE,
            DependencyCount = dependencies.Length,
        };

        var tempArray = ArrayPool<byte>.Shared.Rent(4096);

        if (dependencies.Length > 0)
        {
            stream.Seek(header.DependenciesOffset, SeekOrigin.Begin);
            for (var i = 0; i < dependencies.Length; i++)
            {
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(tempArray.AsSpan(0, 16)), dependencies.Span[i]);
                await stream.WriteAsync(tempArray.AsMemory(0, 16), token);
            }
        }

        header.SettingsOffset = stream.Position;

        // TODO: We can use source generator to generate optimized serializer for settings.
        // For now, we just use reflection for simplicity.

        if (settings is not null)
        {
            var properties = typeof(TSetting).GetProperties();

            if (properties.Length > 0)
            {
                using var bw = new BinaryWriter(stream);

                for (var i = 0; (i < properties.Length); i++)
                {
                    var property = properties[i];
                    var value = property.GetValue(settings);
                }
            }
        }
    }
}
