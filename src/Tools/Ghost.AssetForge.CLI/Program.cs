using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Misaki.CommandLine.Args;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.AssetForge.CLI;

internal class BakeOptions
{
    [Option("asset-dir", Description = "The directory containing the assets to bake. Use semicolon to separate multiple directories.", IsRequired = true)]
    public string AssetDirs { get; set; } = string.Empty;

    [Option("cache-dir", Description = "The directory to use for caching baked assets.", IsRequired = true)]
    public string CacheDir { get; set; } = string.Empty;

    [Option("build-dir", Description = "The directory to use for building the project.", IsRequired = true)]
    public string BuildDir { get; set; } = string.Empty;

    [Option("shader-metadata", Description = "The path to the shader metadata file(s). Use semicolon to separate multiple files.", IsRequired = true)]
    public string ShaderMetadataPaths { get; set; } = string.Empty;
}

internal enum MetdataCommandMode
{
    Update,
    Validate
}

internal class MetadataOptions
{
    [Option("mode", Description = "The mode to run the metadata command in. Can be 'update' or 'validate'.", IsRequired = true)]
    public MetdataCommandMode Mode { get; set; }
    [Option("asset-dir", Description = "The directory containing the assets to bake. Use semicolon to separate multiple directories.", IsRequired = true)]
    public string AssetDirs { get; set; } = string.Empty;
}

public class Program
{
    public static async Task Main(string[] args)
    {
        Logger.Impl.OnLogAdded += static log => Console.WriteLine($"[{log.Level}] {log.Message}");

        var root = CommandBuilder.Create("root")
            .AddSubCommand(
                CommandBuilder.Create("bake")
                .AddOption<BakeOptions>()
                .SetAsyncCommandAction(BuildCommandActionAsync)
                .Build())
            .AddSubCommand(
                CommandBuilder.Create("metadata")
                .AddOption<MetadataOptions>()
                .SetAsyncCommandAction(BuildMetadataCommandActionAsync)
                .Build())
            .Build();

        AllocationManager.Initialize();

        try
        {
            await root.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            AllocationManager.Dispose();
        }
    }

    private static async Task BuildCommandActionAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var opts = context.Bind<BakeOptions>();
        var assetDirs = opts.AssetDirs.Split(';');
        var shaderMetadataPaths = opts.ShaderMetadataPaths.Split(';');

        using var registry = new BakerRegistry();

        var projectService = new ProjectService(registry);
        projectService.InitializeFromArgs(assetDirs, opts.CacheDir, opts.BuildDir, shaderMetadataPaths);
        var projContext = projectService.GetContext();

        var bakeService = new BakeService(projContext, registry);
        var packService = new PackService(projContext, registry);

        Console.WriteLine($"Starting asset bake & pack pipeline...");
        Console.WriteLine($"Assets: {string.Join(", ", assetDirs)}");
        Console.WriteLine($"Cache: {opts.CacheDir}");
        Console.WriteLine($"Build: {opts.BuildDir}");
        Console.WriteLine($"Shader Metadata: {string.Join(", ", shaderMetadataPaths)}");

        var bakeResult = await bakeService.BakeProjectAsync(cancellationToken);
        if (bakeResult.Failed > 0)
        {
            Console.WriteLine($"Bake failed: {bakeResult.Failed} of {bakeResult.Total} assets failed.");
            foreach (var failedAsset in bakeResult.FailedAssets)
            {
                Console.WriteLine($"  Failed: {failedAsset}");
            }
            Environment.Exit(1);
        }

        await packService.PackProjectAsync(cancellationToken);

        Console.WriteLine("Asset bake & pack complete.");
    }

    private static async Task BuildMetadataCommandActionAsync(CommandContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine("Metadata action is not supported yet.");
        return;

        // var opts = context.Bind<MetadataOptions>();
        // 
        // using var registry = new BakerRegistry();
        // 
        // switch (opts.Mode)
        // {
        //     case MetdataCommandMode.Update:
        //         break;
        //     case MetdataCommandMode.Validate:
        //         break;
        //     default:
        //         break;
        // }
    }
}
