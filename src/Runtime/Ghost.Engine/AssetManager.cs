using Ghost.Core;
using System.Runtime.InteropServices;

namespace Ghost.Engine;

internal abstract class RuntimeAsset;

internal interface IRuntimeAssetLoader
{
    ValueTask<Result<RuntimeAsset>> LoadAsync(Stream cookedData, Guid id, CancellationToken token = default);
}

internal sealed class RuntimeLoaderRegistry
{
    private readonly Dictionary<Guid, IRuntimeAssetLoader> _loaders = new();
    public void Register(Guid cookedTypeId, IRuntimeAssetLoader loader)
    {
        _loaders[cookedTypeId] = loader;
    }
    public IRuntimeAssetLoader? GetLoader(Guid cookedTypeId)
    {
        _loaders.TryGetValue(cookedTypeId, out var loader);
        return loader;
    }
}

internal sealed class CookedTextureLoader : IRuntimeAssetLoader
{
    public static readonly Guid TYPE_ID = TextureAsset.s_typeGuid;
    public async ValueTask<Result<RuntimeAsset>> LoadAsync(Stream cookedData, Guid id, CancellationToken token)
    {
        // Read the ImageContentHeader you wrote during import
        var header = new ImageContentHeader();
        cookedData.ReadExactly(MemoryMarshal.AsBytes(new Span<ImageContentHeader>(ref header)));
        // Read the rest as raw GPU data (DDS/BC compressed bytes)
        var data = new byte[cookedData.Length - cookedData.Position];
        await cookedData.ReadExactlyAsync(data, token);
        return new TextureAsset(data, header, id);
    }
}

public class AssetManager
{
}
