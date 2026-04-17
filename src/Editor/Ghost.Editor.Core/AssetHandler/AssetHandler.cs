using Ghost.Core;

namespace Ghost.Editor.Core.AssetHandler;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAssetHandlerAttribute : Attribute
{
    public CustomAssetHandlerAttribute(string id, string[] supportedExtensions, int version = 1)
    {
    }
}

public interface IAssetExportOptions;

public interface IAssetHandler
{
    bool CanExport => false;

    IAssetSettings? CreateDefaultSettings();
    ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default);
    ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default);
}