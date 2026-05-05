using Ghost.Core;
using Ghost.Engine;

namespace Ghost.Editor.Core.Assets;

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

    public IAssetSettings? Settings
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

    ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default);
}

public interface IImportableAssetHandler : IAssetHandler
{
    bool CanExport { get; }
    ValueTask<Result<ImportedSubAsset[]>> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> ExportAsync(string assetPath, string targetPath, IAssetExportOptions? options, CancellationToken token = default);
}

public readonly record struct ImportedSubAsset(Guid Guid, string Kind, string DisplayName, string StablePath, string VirtualSourcePath, Guid HandlerTypeId);

public interface IPackableAssetHandler : IAssetHandler
{
    ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default);
}
