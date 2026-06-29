using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.AssetForge.CLI;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "bake")
        {
            Console.WriteLine("Usage: Ghost.AssetForge.CLI bake --asset-dir <dir> --cache-dir <dir> --build-dir <dir>");
            return;
        }

        var assetDirs = new List<string>();
        string? cacheDir = null;
        string? buildDir = null;
        var shaderMetadataPaths = new List<string>();

        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--asset-dir" && i + 1 < args.Length)
            {
                var paths = args[++i].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                assetDirs.AddRange(paths);
            }
            else if (args[i] == "--cache-dir" && i + 1 < args.Length)
            {
                cacheDir = args[++i];
            }
            else if (args[i] == "--build-dir" && i + 1 < args.Length)
            {
                buildDir = args[++i];
            }
            else if (args[i] == "--shader-metadata" && i + 1 < args.Length)
            {
                // Can be semicolon separated from MSBuild
                var paths = args[++i].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                shaderMetadataPaths.AddRange(paths);
            }
        }

        if (assetDirs.Count == 0 || cacheDir == null || buildDir == null)
        {
            Console.WriteLine("Error: Missing required directory arguments.");
            return;
        }

        AllocationManager.Initialize();
        Logger.Impl.OnLogAdded += log => Console.WriteLine($"[{log.Level}] {log.Message}");

        try
        {
            using var registry = new BakerRegistry();
            var projectService = new ProjectService(registry);
            projectService.InitializeFromArgs(assetDirs, cacheDir, buildDir, shaderMetadataPaths);

            var bakeService = new BakeService(projectService, registry);
            var packService = new PackService(projectService, registry);

            Console.WriteLine($"Starting asset bake & pack pipeline...");
            Console.WriteLine($"Assets: {string.Join(", ", assetDirs)}");
            Console.WriteLine($"Cache: {cacheDir}");
            Console.WriteLine($"Build: {buildDir}");

            await bakeService.BakeProjectAsync();
            await packService.PackProjectAsync();

            Console.WriteLine("Asset bake & pack complete.");
        }
        finally
        {
            AllocationManager.Dispose();
        }
    }
}
