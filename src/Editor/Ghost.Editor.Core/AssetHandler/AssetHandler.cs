using Ghost.Core;
using Ghost.Editor.Core.Contracts;

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
    ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, Guid id, IAssetRegistry assetRegistry, CancellationToken token = default);
    ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry assetRegistry, CancellationToken token = default);
}

public interface IImportableAssetHandler : IAssetHandler
{
    IAssetSettings? CreateDefaultSettings();
    ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default);
}

public interface IExportableAssetHandler : IAssetHandler
{
    ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default);
}

public static class AssetHandlerExtensions
{
    public static async ValueTask<Result> ImportAsync(this IImportableAssetHandler handler, string sourceFilePath, string targetFilePath, Guid id, IAssetSettings? settings = null, CancellationToken token = default)
    {
        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        return await handler.ImportAsync(sourceStream, targetStream, id, settings, token);
    }

    public static async ValueTask<Result> ExportAsync(this IExportableAssetHandler handler, string assetFilePath, string targetFilePath, IAssetExportOptions? options, CancellationToken token = default)
    {
        await using var assetStream = new FileStream(assetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        return await handler.ExportAsync(assetStream, targetStream, options, token);
    }

    public static async ValueTask<Result<Asset>> LoadAsync(this IAssetHandler handler, string assetFilePath, Guid id, IAssetRegistry assetDatabase, CancellationToken token = default)
    {
        await using var sourceStream = new FileStream(assetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await handler.LoadAsync(sourceStream, id, assetDatabase, token);
    }
}
