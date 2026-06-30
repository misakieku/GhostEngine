using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.DSL.Models;

namespace Ghost.AssetForge.Core.Services;

public class BakeService
{
    private readonly ProjectService _projectService;
    private readonly BakerRegistry _bakerRegistry;

    private readonly AssetBakerContext _bakerContext;

    public BakeService(ProjectService projectService, BakerRegistry bakerRegistry)
    {
        _projectService = projectService;
        _bakerRegistry = bakerRegistry;

        var shaderData = new ShaderMetadata();
        
        foreach (var path in _projectService.ShaderMetadataPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var deserialized = System.Text.Json.JsonSerializer.Deserialize<ShaderMetadata>(json);
                    if (deserialized != null)
                    {
                        shaderData.Merge(deserialized);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load shader metadata from {path}: {ex.Message}");
                }
            }
        }

        _bakerContext = new AssetBakerContext
        {
            ShderMetadata = shaderData,
            AssetDirectories = _projectService.AssetDirectories
        };
    }

    public event Action<int, int>? OnProgress;

    public async Task BakeProjectAsync(CancellationToken cancellationToken = default)
    {
        var project = _projectService.CurrentProject ?? throw new InvalidOperationException("No project loaded.");
        var cacheDir = _projectService.CacheDirectory;

        // Map VirtualPath -> AbsolutePath. Later directories overwrite earlier ones.
        var virtualPathToFile = new Dictionary<string, string>();
        
        foreach (var assetDir in _projectService.AssetDirectories)
        {
            if (!Directory.Exists(assetDir))
                continue;
            
            var filesInDir = Directory.GetFiles(assetDir, "*.*", SearchOption.AllDirectories)
                                      .Where(f => !f.EndsWith(".meta"));
            
            foreach (var file in filesInDir)
            {
                var relativePath = Path.GetRelativePath(assetDir, file);
                var virtualPath = relativePath.Replace('\\', '/');
                virtualPathToFile[virtualPath] = file;
            }
        }

        if (virtualPathToFile.Count == 0)
        {
            Logger.Error("No files found in any Asset directory.");
            return;
        }

        // Duplicate check (by basename) to ensure no two assets map to the same name
        var baseNames = new HashSet<string>();
        foreach (var kvp in virtualPathToFile)
        {
            var virtualPath = kvp.Key;
            var dir = Path.GetDirectoryName(virtualPath) ?? string.Empty;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(virtualPath);
            var key = Path.Combine(dir, nameWithoutExt).Replace('\\', '/');

            if (!baseNames.Add(key))
            {
                var err = $"Fatal Error: Duplicate asset name '{key}' found in folder.";
                Logger.Error(err);
                throw new InvalidOperationException(err);
            }
        }

        var completed = 0;
        var total = virtualPathToFile.Count;
        OnProgress?.Invoke(completed, total);

        foreach (var kvp in virtualPathToFile)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = kvp.Key;
            var sourceFile = kvp.Value;
            var destPath = Path.Combine(cacheDir, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir != null && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Path to cache file without extension (cache name = original name without extension)
            var ext = Path.GetExtension(sourceFile);
            var cacheFile = Path.Combine(destDir ?? cacheDir, Path.GetFileNameWithoutExtension(sourceFile));
            var metaFile = sourceFile + ".meta";

            var needsBake = true;
            if (File.Exists(cacheFile) && File.Exists(metaFile))
            {
                var fileinfo = new FileInfo(cacheFile);
                if (fileinfo.Length != 0)
                {
                    var srcTime = File.GetLastWriteTimeUtc(sourceFile);
                    var metaTime = File.GetLastWriteTimeUtc(metaFile);
                    var cacheTime = File.GetLastWriteTimeUtc(cacheFile);

                    if (cacheTime >= srcTime && cacheTime >= metaTime)
                    {
                        needsBake = false;
                    }
                }
            }
            
            if (needsBake)
            {
                Logger.Info($"Baking {relativePath}...");
                var metadata = _projectService.LoadMetadata(metaFile);

                var baker = _bakerRegistry.GetBaker(ext);
                if (baker == null)
                {
                    Logger.Warning($"No baker for {ext}. Skip.");
                    continue;
                }

                if (metadata == null)
                {
                    metadata = new AssetMetadata
                    {
                        Id = Guid.NewGuid(),
                        Type = _bakerRegistry.DetectAssetType(ext),
                        Settings = _bakerRegistry.CreateDefaultSettings(ext)
                    };

                    _projectService.SaveMetadata(metaFile, metadata);
                    Logger.Info($"Created default metadata for {relativePath}.");
                }

                if (metadata.Settings == null)
                {
                    Logger.Error($"Missing settings in metadata for {sourceFile}. Skip.");
                    continue;
                }

                await using var fs = new FileStream(cacheFile, FileMode.Create, FileAccess.Write);
                await baker.BakeAssetAsync(sourceFile, fs, metadata.Settings, _bakerContext, cancellationToken);
            }
            else
            {
                Logger.Info($"Skipping {relativePath} (Up to date)");
            }

            completed++;
            OnProgress?.Invoke(completed, total);
        }

        Logger.Info("Baking complete.");
    }
}
