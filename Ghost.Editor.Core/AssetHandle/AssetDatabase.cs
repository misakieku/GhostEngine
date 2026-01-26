using Ghost.Data.Services;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    private static FileSystemWatcher? s_watcher;

    public static DirectoryInfo? AssetsDirectory
    {
        get;
        private set;
    }

    internal static void Initialize()
    {
        if (ProjectService.CurrentProject.Metadata == null)
        {
            throw new InvalidOperationException("Project metadata is not initialized. Ensure that the project is loaded before accessing the AssetDatabase.");
        }

        AssetsDirectory = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(ProjectService.CurrentProject.Path)!, ProjectService.ASSETS_FOLDER));
        s_watcher = new FileSystemWatcher
        {
            Path = AssetsDirectory.FullName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        InitializeAssetHandle();
        InitializeMetaData();
    }
}
