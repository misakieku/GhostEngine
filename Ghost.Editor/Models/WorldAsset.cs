using Ghost.Editor.AssetHandle;
using Ghost.Editor.Resources;
using Ghost.Editor.SceneGraph;

namespace Ghost.Editor.Models;

public class WorldAsset : Asset
{
    [AsyncAssetOpenHandler(FileExtensions.SCENE_FILE_EXTENSION)]
    public static async Task Open(string path)
    {
        await EditorWorldManager.LoadWorld(path);
    }
}