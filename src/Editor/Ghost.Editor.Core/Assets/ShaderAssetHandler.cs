using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.Engine;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Assets;

[Guid(GUID)]
public sealed partial class GraphicsShaderAsset : IAsset
{
    public const string GUID = "7BD4591C-B017-4814-AA0B-3F30EB3E727E";

    public Guid ID
    {
        get;
    }

    public IAssetSettings? Settings
    {
        get;
    }

    public Guid TypeID => typeof(GraphicsShaderAsset).GUID;

    public GraphicsShaderDescriptor Descriptor
    {
        get;
    }

    internal GraphicsShaderAsset(GraphicsShaderDescriptor descriptor, Guid id)
    {
        ID = id;
        Descriptor = descriptor;
    }

    public void Dispose()
    {
    }
}

[Guid(GUID)]
public sealed partial class ComputeShaderAsset : IAsset
{
    public const string GUID = "EA881979-CD8D-4088-B568-D42645F18C2A";

    public Guid ID
    {
        get;
    }

    public IAssetSettings? Settings
    {
        get;
    }

    public Guid TypeID => typeof(ComputeShaderAsset).GUID;

    public ComputeShaderDescriptor Descriptor
    {
        get;
    }

    internal ComputeShaderAsset(ComputeShaderDescriptor descriptor, Guid id)
    {
        ID = id;
        Descriptor = descriptor;
    }

    public void Dispose()
    {
    }
}

// Shader does not handle import/export via asset registry, it will handled by the hot reload system.
[CustomAssetHandler(GraphicsShaderAsset.GUID, [".gshdr"], 1)]
internal class GraphicsShaderAssetHandler : IPackableAssetHandler
{
    public AssetType RuntimeAssetType => AssetType.Shader;
    public Guid EditorAssetTypeID => typeof(GraphicsShaderAsset).GUID;

    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        return null;
    }

    public async ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
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

    public ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Saving shader assets is not supported yet as it's read-only. Please edit the shader source file directly if you need to modify it."));
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Packing shader assets is not supported yet."));
    }
}

[CustomAssetHandler(ComputeShaderAsset.GUID, [".gcomp"], 1)]
internal class ComputeShaderAssetHandler : IPackableAssetHandler
{
    public AssetType RuntimeAssetType => AssetType.Shader;
    public Guid EditorAssetTypeID => typeof(ComputeShaderAsset).GUID;

    public IAssetSettings? CreateDefaultSettings(string ext)
    {
        return null;
    }

    public async ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
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

    public ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Saving shader assets is not supported yet as it's read-only. Please edit the shader source file directly if you need to modify it."));
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        return new ValueTask<Result>(Result.Failure("Packing shader assets is not supported yet."));
    }
}