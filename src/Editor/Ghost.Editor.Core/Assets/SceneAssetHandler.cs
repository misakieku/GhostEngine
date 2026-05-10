using Ghost.Core;
using Ghost.Editor.Core.Services;
using Ghost.Engine;

namespace Ghost.Editor.Core.Assets;

[CustomAssetHandler(AssetTypeId = SceneAsset.GUID, RuntimeAssetType = AssetType.Scene, Extensions = new[] { ".gscene" })]
internal class SceneAssetHandler : IImportableAssetHandler, IPackableAssetHandler
{
    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        return new SceneAssetSettings();
    }

    public async ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            if (!File.Exists(assetPath))
            {
                return Result.Failure("Scene file does not exist.");
            }

            var data = await SceneSerializationService.DeserializeSceneFileAsync(assetPath, token);
            var asset = new SceneAsset(id, settings)
            {
                SceneName = Path.GetFileNameWithoutExtension(assetPath),
                EntityCount = data?.Entities?.Count ?? 0,
            };

            return Result.Success<IAsset>(asset);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default)
    {
        if (asset is not SceneAsset sceneAsset)
        {
            return ValueTask.FromResult(Result.Failure("Asset type is not SceneAsset"));
        }

        return ValueTask.FromResult(Result.Failure("Scene saving is handled by SceneSerializationService directly."));
    }

    public async ValueTask<Result<ImportedSubAsset[]>> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                return Result.Failure("Source scene file does not exist.");
            }

            var data = await SceneSerializationService.DeserializeSceneFileAsync(sourcePath, token);
            if (data == null)
            {
                return Result.Failure("Failed to deserialize scene file.");
            }

            using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            SceneSerializationService.SerializeToBinary(data, stream);

            return Result.Success(Array.Empty<ImportedSubAsset>());
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to import scene asset: {ex.Message}");
        }
    }

    public async ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        try
        {
            if (!File.Exists(assetPath))
            {
                return Result.Failure("Scene file does not exist.");
            }

            var data = await SceneSerializationService.DeserializeSceneFileAsync(assetPath, token);
            if (data == null)
            {
                return Result.Failure("Failed to deserialize scene file.");
            }

            SceneSerializationService.SerializeToBinary(data, targetStream);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to pack scene asset: {ex.Message}");
        }
    }
}
