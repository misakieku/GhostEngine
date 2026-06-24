using Ghost.Core;

namespace Ghost.Engine.Streaming;

internal class RuntimeContentProvider : IContentProvider
{
    public AssetType GetAssetType(Guid guid)
    {
        throw new NotImplementedException();
    }

    public Guid[] GetDependencies(Guid guid)
    {
        throw new NotImplementedException();
    }

    public bool HasAsset(Guid guid)
    {
        throw new NotImplementedException();
    }

    public Result<Stream> OpenRead(Guid guid, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
