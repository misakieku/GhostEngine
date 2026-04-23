using Ghost.Core;
using Ghost.Engine;

namespace Ghost.Editor.Core.AssetHandler;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAssetHandlerAttribute : Attribute
{
    public CustomAssetHandlerAttribute(string assetTypeID, string[] supportedExtensions, int version = 1)
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
    AssetType RuntimeAssetType { get; }
    Guid EditorAssetTypeID { get; }

    IAssetSettings? CreateDefaultSettings();

    ValueTask<Result<IAsset>> LoadAssetAsync(FileStream assetStream, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(FileStream targetStream, IAsset asset, CancellationToken token = default);
}

public interface IImportableAssetHandler : IAssetHandler
{
    bool CanExport { get; }
    ValueTask<Result> ImportAsync(FileStream sourceStream, FileStream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> ExportAsync(FileStream assetStream, FileStream targetStream, IAssetExportOptions? options, CancellationToken token = default);
}

public interface IPackableAssetHandler : IAssetHandler
{
    ValueTask<Result> PackAsync(FileStream assetStream, Stream targetStream, CancellationToken token = default);
}