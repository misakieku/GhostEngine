using Ghost.Core;
using Ghost.Engine;

namespace Ghost.Editor.Core.AssetHandler;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAssetHandlerAttribute : Attribute
{
    public CustomAssetHandlerAttribute(string TypeID, string[] supportedExtensions, int version = 1)
    {
    }
}

public interface IAsset : IDisposable
{
    public Guid ID
    {
        get;
    }

    public Guid TypeID
    {
        get;
    }

    public IAssetSettings Settings
    {
        get;
    }
}

public interface IAssetExportOptions;

public interface IAssetHandler
{
    bool CanExport => false;
    AssetType TargetAssetType { get; }

    IAssetSettings? CreateDefaultSettings();

    ValueTask<Result<IAsset>> LoadAssetAsync(Stream assetStream, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(Stream targetStream, IAsset asset, CancellationToken token = default);
    ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default);
}
