using Ghost.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Engine;
using Ghost.Engine.Streaming;

namespace Ghost.Editor.Core.Assets;

// FIX: This is broken in many ways.
[CustomAssetHandler(AssetTypeId = SceneAsset.GUID, RuntimeAssetType = AssetType.Scene, Extensions = new[] { ".gscene" })]
internal class SceneAssetHandler : IImportableAssetHandler, IPackableAssetHandler
{
    [AssetOpenHandler(".gscene")]
    private static async Task<Result> OpenAsync(string path)
    {
        var assetRegistry = EditorApplication.GetService<IAssetRegistry>();
        var guid = assetRegistry.GetAssetGuid(path);
        var result = await assetRegistry.LoadAssetAsync(guid);
        if (result.IsFailure)
        {
            return result;
        }

        var worldService = EditorApplication.GetService<IEditorWorldService>();
        try
        {
            var scene = await worldService.OpenSceneAsync(guid);
            ((SceneAsset)result.Value).RuntimeSceneID = scene.ID;
            worldService.RegisterSceneAsset(scene.ID, (SceneAsset)result.Value);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to open scene: {ex.Message}");
        }
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

    public async ValueTask<Result<AssetImportResult>> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                return Result.Failure<AssetImportResult>("Source scene file does not exist.");
            }

            var data = await SceneSerializationService.DeserializeSceneFileAsync(sourcePath, token);
            if (data == null)
            {
                return Result.Failure<AssetImportResult>("Failed to deserialize scene file.");
            }

            using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var dependencies = SceneSerializationService.SerializeToBinary(data, stream);

            return new AssetImportResult(Array.Empty<ImportedSubAsset>(), dependencies);
        }
        catch (Exception ex)
        {
            return Result.Failure<AssetImportResult>($"Failed to import scene asset: {ex.Message}");
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
