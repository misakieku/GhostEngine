using Ghost.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Engine;
using Ghost.Engine.Streaming;

namespace Ghost.Editor.Core.Assets;

[CustomAssetHandler(AssetTypeId = SceneAsset.GUID, RuntimeAssetType = AssetType.Scene, Extensions = new[] { ".gscene" })]
internal class SceneAssetHandler : IImportableAssetHandler, IPackableAssetHandler
{
    [AssetOpenHandler(".gscene")]
    private static async Task<Result> OpenAsync(string path)
    {
        // Actually double clicking the asset in content browser will just open it.
        // We probably shouldn't do the actual loading in OpenAsync, but let's keep it simple for now.
        // OpenAsync usually returns immediately if there's no UI, or we should use AssetRegistry.LoadAssetAsync
        var assetRegistry = EditorApplication.GetService<IAssetRegistry>();
        await assetRegistry.LoadAssetAsync(assetRegistry.GetAssetGuid(path));
        // AssetMeta handles this. This method is just a quick hack for double clicking.
        //var data = await SceneSerializationService.DeserializeSceneFileAsync(path);
        //if (data == null)
        //{
        //    return Result.Failure("Failed to load scene.");
        //}

        //var service = EditorApplication.GetService<SceneSerializationService>();
        //service.LoadSceneIntoEditorWorld(data, SceneLoadingType.Single, null);
        return Result.Success();
    }

    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        return new SceneAssetSettings();
    }

    public async ValueTask<Result<Asset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
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
                RuntimeSceneID = Engine.Core.Scene.INVALID_ID // Default
            };

            if (data != null)
            {
                var tcs = new TaskCompletionSource<Asset>();
                var service = EditorApplication.GetService<SceneSerializationService>();
                service.LoadSceneIntoEditorWorld(data, SceneLoadingType.Single, (scene) =>
                {
                    asset.RuntimeSceneID = scene.ID;
                    EditorApplication.GetService<IEditorWorldService>().RegisterSceneAsset(scene.ID, asset);
                    tcs.TrySetResult(asset);
                });
                return Result.Success(await tcs.Task);
            }

            return Result.Success<Asset>(asset);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async ValueTask<Result> SaveAssetAsync(string targetPath, Asset asset, CancellationToken token = default)
    {
        if (asset is not SceneAsset sceneAsset)
        {
            return Result.Failure("Asset type is not SceneAsset");
        }

        var worldService = EditorApplication.GetService<IEditorWorldService>();
        var tcs = new TaskCompletionSource<byte[]>();

        worldService.Defer(() =>
        {
            try
            {
                var scene = Engine.Core.Scene.FromID(sceneAsset.RuntimeSceneID);
                var service = EditorApplication.GetService<SceneSerializationService>();
                var bytes = service.SerializeSceneToMemory(scene);
                tcs.TrySetResult(bytes);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            var bytes = await tcs.Task;
            await File.WriteAllBytesAsync(targetPath, bytes, token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to save scene: {ex.Message}");
        }
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
