using Ghost.AssetForge.Core.Generator;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Misaki.CommandLine.Args;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Text.Json;

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

    [Option("force-shader-debug", Description = "Whether to force shader to keep debug information in the baked assets. Default is false.", IsRequired = false)]
    public bool ForceShaderDebugInfo { get; set; } = false;

    [Option("target-graphics-api", Description = "The target graphics API for the baked assets. Default is 'DirectX'.", IsRequired = false)]
    public string TargetGraphicsAPI { get; set; } = "DirectX";
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
    [Option("paths", Description = "The path to the directory containing the assets or the path to the asset file. Use semicolon to separate multiple paths.", IsRequired = true)]
    public string Paths { get; set; } = string.Empty;
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
            .AddSubCommand(
                CommandBuilder.Create("generate")
                .AddSubCommand(
                    CommandBuilder.Create("pbr-lut")
                    .AddOption("size", (builder) => builder
                        .WithDescription("The size of the PBR LUT.")
                        .WithValueType(typeof(int))
                        .WithRequired())
                    .AddOption("output", (builder) => builder
                        .WithDescription("The output path of the generated PBR LUT.")
                        .WithValueType(typeof(string))
                        .WithRequired())
                    .SetAsyncCommandAction(static (ctx, ct) =>
                    {
                        ctx.Options.TryGetValue("size", out var sizeObj);
                        ctx.Options.TryGetValue("output", out var outputObj);
                        var size = (int)sizeObj.value;
                        var output = (string)outputObj.value;

                        if (size <= 0)
                        {
                            throw new ArgumentException("Size must be greater than 0.");
                        }

                        return PbrLutGenerator.GenerateAsync(size, output, ct);
                    })
                    .Build())
                .Build())
            .Build();

        AllocationManager.Initialize();

        try
        {
            await root.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex.Message}");
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
        var packService = new PackService(projContext);

        Logger.Info($"Starting asset bake & pack pipeline...");
        Logger.Info($"Assets: {string.Join(", ", assetDirs)}");
        Logger.Info($"Cache: {opts.CacheDir}");
        Logger.Info($"Build: {opts.BuildDir}");
        Logger.Info($"Shader Metadata: {string.Join(", ", shaderMetadataPaths)}");
        Console.WriteLine("=================================================");

        var config = new BakeConfig
        {
            ForceShaderDebugInfo = opts.ForceShaderDebugInfo,
            TargetGraphicsAPI = opts.TargetGraphicsAPI
        };

        var bakeResult = await bakeService.BakeProjectAsync(config, cancellationToken);
        if (bakeResult.Failed > 0)
        {
            Logger.Error($"Bake failed: {bakeResult.Failed} of {bakeResult.Total} assets failed.");
            foreach (var failedAsset in bakeResult.FailedAssets)
            {
                Logger.Error($"  Failed: {failedAsset}");
            }

            Environment.Exit(1);
        }

        if (bakeResult.Total == bakeResult.Skipped)
        {
            Logger.Info("All assets were skipped.");
            return;
        }

        await packService.PackProjectAsync(cancellationToken);

        Logger.Info("Asset bake & pack complete.");
    }

    private static async Task BuildMetadataCommandActionAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var opts = context.Bind<MetadataOptions>();

        if (opts.Mode == MetdataCommandMode.Validate)
        {
            Logger.Error("Metadata validation is not supported yet.");
            return;
        }

        using var registry = new BakerRegistry();

        foreach (var path in opts.Paths.Split(';'))
        {
            var isFile = File.Exists(path);
            var isDirectory = Directory.Exists(path);
            if (!isFile && !isDirectory)
            {
                Logger.Warning($"Asset path '{path}' does not exist. Skipping.");
            }

            if (isFile)
            {
                ProcessMetadataFile(opts, registry, path);
            }
            else if (isDirectory)
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    ProcessMetadataFile(opts, registry, file);
                }
            }
        }
    }

    private static void ProcessMetadataFile(MetadataOptions opts, BakerRegistry registry, string file)
    {
        var ext = Path.GetExtension(file);

        if (ext.Equals(".meta", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var metaFilePath = file + ".meta";

        var version = registry.GetSettingsVersion(ext);
        if (version == -1)
        {
            Logger.Warning($"No registered baker for file '{file}' with extension '{ext}'. Skipping.");
            return;
        }

        AssetMetadata? metadata;
        if (File.Exists(metaFilePath))
        {
            metadata = JsonSerializer.Deserialize<AssetMetadata>(File.ReadAllText(metaFilePath), ProjectContext.JsonOptions);
            if (metadata != null)
            {
                switch (opts.Mode)
                {
                    case MetdataCommandMode.Update:
                        if (metadata.Settings != null && metadata.Settings.Version < version)
                        {
                            Logger.Info($"Updating metadata file '{metaFilePath}' from version {metadata.Settings.Version} to {version}.");
                            metadata.Settings.Version = version;
                            File.WriteAllText(metaFilePath, JsonSerializer.Serialize(metadata, ProjectContext.JsonOptions));
                        }

                        break;
                    case MetdataCommandMode.Validate:
                        break;
                    default:
                        break;
                }

                return;
            }

            Logger.Error($"Failed to deserialize metadata file '{metaFilePath}'.");
            File.Delete(metaFilePath);
        }

        metadata = new AssetMetadata
        {
            Id = Guid.NewGuid(),
            Type = registry.DetectAssetType(ext),
            Settings = registry.CreateDefaultSettings(ext)
        };

        Logger.Info($"Creating new metadata file '{metaFilePath}' with version {version}.");
        File.WriteAllText(metaFilePath, JsonSerializer.Serialize(metadata, ProjectContext.JsonOptions));
    }
}
