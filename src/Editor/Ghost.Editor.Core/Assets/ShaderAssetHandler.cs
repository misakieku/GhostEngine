using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.Engine.Streaming;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Assets;

[Guid(GUID)]
public sealed partial class GraphicsShaderAsset : Asset
{
    public const string GUID = "7BD4591C-B017-4814-AA0B-3F30EB3E727E";

    public GraphicsShaderDescriptor Descriptor
    {
        get;
    }

    internal GraphicsShaderAsset(GraphicsShaderDescriptor descriptor, Guid id)
        : base(id, typeof(GraphicsShaderAsset).GUID, null)
    {
        Descriptor = descriptor;
    }
}

[Guid(GUID)]
public sealed partial class ComputeShaderAsset : Asset
{
    public const string GUID = "EA881979-CD8D-4088-B568-D42645F18C2A";

    public ComputeShaderDescriptor Descriptor
    {
        get;
    }

    internal ComputeShaderAsset(ComputeShaderDescriptor descriptor, Guid id)
        : base(id, typeof(ComputeShaderAsset).GUID, null)
    {
        Descriptor = descriptor;
    }
}

// Shader does not handle import/export via asset registry, it will handled by the hot reload system.
[CustomAssetHandler(AssetTypeId = GraphicsShaderAsset.GUID, RuntimeAssetType = AssetType.Shader, Extensions = new[] { ".gshdr" })]
internal class GraphicsShaderAssetHandler : IPackableAssetHandler
{
    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        return null;
    }

    public async ValueTask<Result<Asset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            var result = DSLShaderCompiler.CompileGraphicsShader(assetPath);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            return new GraphicsShaderAsset(result.Value, id);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to load shader asset: {ex.Message}");
        }
    }

    public ValueTask<Result> SaveAssetAsync(string targetPath, Asset asset, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Saving shader assets is not supported yet as it's read-only. Please edit the shader source file directly if you need to modify it."));
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Packing shader assets is not supported yet."));
    }
}

[CustomAssetHandler(AssetTypeId = ComputeShaderAsset.GUID, RuntimeAssetType = AssetType.Shader, Extensions = new[] { ".gcomp" })]
internal class ComputeShaderAssetHandler : IPackableAssetHandler
{
    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        return null;
    }

    public async ValueTask<Result<Asset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            var result = DSLShaderCompiler.CompileComputeShaderCode(assetPath);
            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            return new ComputeShaderAsset(result.Value, id);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to load shader asset: {ex.Message}");
        }
    }

    public ValueTask<Result> SaveAssetAsync(string targetPath, Asset asset, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Saving shader assets is not supported yet as it's read-only. Please edit the shader source file directly if you need to modify it."));
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Packing shader assets is not supported yet."));
    }
}