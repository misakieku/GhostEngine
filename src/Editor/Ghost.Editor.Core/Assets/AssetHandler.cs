using Ghost.Core;
using Ghost.Engine.Streaming;

namespace Ghost.Editor.Core.Assets;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAssetHandlerAttribute : Attribute
{
    public required string AssetTypeId
    {
        get; set;
    }

    public required AssetType RuntimeAssetType
    {
        get; set;
    }

    public required string[] Extensions
    {
        get; set;
    }

    public int Version
    {
        get; set;
    } = 1;

    public bool AllowCaching
    {
        get; set;
    } = true;
}

public abstract class IAsset : GhostObject
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

    protected IAsset(Guid id, Guid typeId, IAssetSettings? settings)
        :base(id)
    {
        ID = id;
        TypeID = typeId;
        Settings = settings;
    }
}

public interface IAssetExportOptions;

public interface IAssetHandler
{
    IAssetSettings? CreateDefaultSettings(string ext);

    ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default);
}

public interface IImportableAssetHandler : IAssetHandler
{
    ValueTask<Result<ImportedSubAsset[]>> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default);
}

public readonly record struct ImportedSubAsset(Guid Guid, string Kind, string DisplayName, string StablePath, string VirtualSourcePath, Guid AssetTypeId);

public interface IPackableAssetHandler : IAssetHandler
{
    ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default);
}
