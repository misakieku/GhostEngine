using Ghost.Core;
using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.AssetHandler;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAssetHandlerAttribute : Attribute
{
    public required string ID
    {
        get; init;
    }

    public required string[] SupportedExtensions
    {
        get; init;
    }

    public bool AllowCaching
    {
        get; init;
    } = true;
}

public interface IAssetExportOptions;

public interface IAssetHandler
{
    ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, IAssetRegistry assetRegistry, CancellationToken token = default);
    ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry assetRegistry, CancellationToken token = default);
}

public interface IImportableAssetHandler : IAssetHandler
{
    ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, CancellationToken token = default);
    ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default);
}

public static class AssetHandlerExtensions
{
    public static async ValueTask<Result> ImportAsync(this IImportableAssetHandler handler, string sourceFilePath, string targetFilePath, Guid id, CancellationToken token = default)
    {
        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        return await handler.ImportAsync(sourceStream, targetStream, id, token);
    }

    public static async ValueTask<Result> ExportAsync(this IImportableAssetHandler handler, string assetFilePath, string targetFilePath, IAssetExportOptions? options, CancellationToken token = default)
    {
        await using var assetStream = new FileStream(assetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        return await handler.ExportAsync(assetStream, targetStream, options, token);
    }

    public static async ValueTask<Result<Asset>> ReadAsync(this IAssetHandler handler, string assetFilePath, IAssetRegistry assetDatabase, CancellationToken token = default)
    {
        await using var sourceStream = new FileStream(assetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await handler.LoadAsync(sourceStream, assetDatabase, token);
    }
}
