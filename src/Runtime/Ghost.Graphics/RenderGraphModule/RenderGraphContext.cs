using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
namespace Ghost.Graphics.RenderGraphModule;

public interface IRenderGraphContext
{
    ResourceManager ResourceManager { get; }
    IResourceDatabase ResourceDatabase { get; }

    float2 RelativeScale { get; }

    Handle<GPUResource> GetActualResource(Identifier<RGResource> resource);
    Handle<GPUTexture> GetActualTexture(Identifier<RGTexture> texture);
    Handle<GPUBuffer> GetActualBuffer(Identifier<RGBuffer> buffer);
}

public interface IRasterRenderContext : IRenderGraphContext
{
    void SetViewport(ViewportDesc desc);
    void SetScissorRect(ScissorRectDesc desc);

    void SetGlobalData(uint globalIndex, uint viewIndex);
    void SetInstanceIndex(uint instanceIndex);

    void SetActiveMaterial(Handle<Material> material);
    void SetActiveMaterial(scoped in Material material);
    void SetActiveMaterialPass(Handle<Material> material, PassSemantic semantic);
    void SetActiveMaterialPass(scoped in Material material, PassSemantic semantic);
    void SetActiveMesh(Handle<Mesh> mesh);
    void SetActiveMesh(scoped in Mesh mesh);
    void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);
}

public interface IComputeRenderContext : IRenderGraphContext
{
    void SetActiveCompute(Handle<ComputeShader> computeShader, int entryIndex);
    void SetActiveShaderPass(Handle<Shader> shader, PassSemantic semantic);
    void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);
}
public interface IUnsafeRenderContext : IRasterRenderContext, IComputeRenderContext
{
    ICommandBuffer GetCommandBufferUnsafe();
}

internal sealed class RenderGraphContext : IUnsafeRenderContext
{
    private readonly ResourceManager _resourceManager;
    private readonly ShaderLibrary _shaderLibrary;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly RenderGraphResourceRegistry _resources;

    private ICommandBuffer _commandBuffer;

    private readonly TextureFormat[] _rtvFormats;
    private TextureFormat _dsvFormat;
    private int _rtvCount;

    private Handle<GPUBuffer> _activePerMaterialData;
    private Handle<GPUBuffer> _activePerMeshData;
    private int _activeMeshIndexCount;

    private uint _activeFrameBuffer;
    private uint _activeViewBuffer;
    private uint _activeInstanceIndex;

    public ResourceManager ResourceManager => _resourceManager;
    public IResourceDatabase ResourceDatabase => _resourceDatabase;

    public int ActiveMeshIndexCount => _activeMeshIndexCount;

    // TODO: Upload relative scale to gpu.
    public float2 RelativeScale
    {
        get; set;
    }

    internal RenderGraphContext(ResourceManager resourceManager, ShaderLibrary shaderLibrary, IResourceDatabase resourceDatabase, IPipelineLibrary pipelineLibrary, RenderGraphResourceRegistry resources)
    {
        _resourceManager = resourceManager;
        _shaderLibrary = shaderLibrary;
        _resourceDatabase = resourceDatabase;
        _pipelineLibrary = pipelineLibrary;
        _resources = resources;

        _commandBuffer = null!;

        _rtvFormats = new TextureFormat[RHIUtility.MAX_RENDER_TARGETS];
        _dsvFormat = TextureFormat.Unknown;
    }

    internal void BeginNewFrame(ICommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
    }

    internal void SetRenderTargetFormats(ReadOnlySpan<TextureFormat> rtvFormats, TextureFormat dsvFormat)
    {
        for (var i = 0; i < RHIUtility.MAX_RENDER_TARGETS; i++)
        {
            _rtvFormats[i] = i < rtvFormats.Length ? rtvFormats[i] : TextureFormat.Unknown;
        }

        _dsvFormat = dsvFormat;
        _rtvCount = rtvFormats.Length;
    }

    public Handle<GPUResource> GetActualResource(Identifier<RGResource> resource)
    {
        if (resource.IsInvalid)
        {
            return Handle<GPUResource>.Invalid;
        }

        return _resources.GetResource(resource).backingResource;
    }

    public Handle<GPUTexture> GetActualTexture(Identifier<RGTexture> texture)
    {
        return _resources.GetResource(texture.AsResource()).backingResource.AsTexture();
    }

    public Handle<GPUBuffer> GetActualBuffer(Identifier<RGBuffer> buffer)
    {
        return _resources.GetResource(buffer.AsResource()).backingResource.AsBuffer();
    }

    public void SetViewport(ViewportDesc desc)
    {
        _commandBuffer.SetViewport(desc);
    }

    public void SetScissorRect(ScissorRectDesc desc)
    {
        _commandBuffer.SetScissorRect(desc);
    }

    public void SetActiveMaterial(Handle<Material> material)
    {
        var r = _resourceManager.GetMaterialReference(material);
        if (r.IsFailure)
        {
            throw InvalidResourceHandleException.Create(material);
        }

        ref readonly var mat = ref r.Value;
        SetActiveMaterial(in mat);
    }

    public void SetActiveMaterial(scoped in Material material)
    {
        var shaderResult = _resourceManager.GetShaderReference(material.Shader);
        if (shaderResult.IsFailure)
        {
            throw InvalidResourceHandleException.Create(material.Shader);
        }

        ref var shader = ref shaderResult.Value;
        ref readonly var pass = ref shader.GetPassReference(material.ActivePassIndex);

        var passPipelineHash = new PassAttachmentHash(_rtvFormats, _dsvFormat);
        var materialPipeline = material.GetPassPipelineOverride(material.ActivePassIndex);

        var (compiledHash, error) = _shaderLibrary.GetCompiledHash(shader.UniqueID, material.ActivePassIndex);
        if (error.IsFailure)
        {
            // TODO: Fallback to a default shader or show an error material.
            return;
        }

        var pipelineKey = RHIUtility.CreateGraphicsPipelineKey(compiledHash, materialPipeline, passPipelineHash);

        if (!_pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            var compiledCacheResult = _shaderLibrary.GetCompiledCache(shader.UniqueID, material.ActivePassIndex);
            if (compiledCacheResult.IsFailure)
            {
                Logger.Warning($"Failed to load compiled shader cache for graphics pipeline {pipelineKey}. Skipping draw call.");
                return;
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 3); // as, ms, ps

            var asByteCode = byteCodes.Slice((int)byteCodeOffsets[0], (int)(byteCodeOffsets[1] - byteCodeOffsets[0]));
            var msByteCode = byteCodes.Slice((int)byteCodeOffsets[1], (int)(byteCodeOffsets[2] - byteCodeOffsets[1]));
            var psByteCode = byteCodes.Slice((int)byteCodeOffsets[2]);

            var psoDes = new GraphicsPSODesc
            {
                CompiledHash = compiledHash,

                PipelineOption = materialPipeline,

                RtvFormats = _rtvFormats.AsSpan(0, _rtvCount),
                DsvFormat = _dsvFormat,

                AsCode = asByteCode,
                MsCode = msByteCode,
                PsCode = psByteCode,
            };

            _pipelineLibrary.CreateGraphicsPipeline(in psoDes).GetValueOrThrow();
        }

        _activePerMaterialData = material._cBufferCache.GpuResource;
        _commandBuffer.SetPipelineState(pipelineKey);
    }
    public void SetActiveMaterialPass(Handle<Material> material, PassSemantic semantic)
    {
        var r = _resourceManager.GetMaterialReference(material);
        if (r.IsFailure)
        {
            throw InvalidResourceHandleException.Create(material);
        }

        ref readonly var mat = ref r.Value;
        SetActiveMaterialPass(in mat, semantic);
    }

    public void SetActiveMaterialPass(scoped in Material material, PassSemantic semantic)
    {
        var shaderResult = _resourceManager.GetShaderReference(material.Shader);
        if (shaderResult.IsFailure)
        {
            throw InvalidResourceHandleException.Create(material.Shader);
        }

        ref var shader = ref shaderResult.Value;
        var passIndex = shader.GetPassIndex(semantic);
        if (passIndex < 0)
        {
            return;
        }

        ref readonly var pass = ref shader.GetPassReference(passIndex);

        var passPipelineHash = new PassAttachmentHash(_rtvFormats, _dsvFormat);
        var materialPipeline = material.GetPassPipelineOverride(passIndex);

        var (compiledHash, error) = _shaderLibrary.GetCompiledHash(shader.UniqueID, passIndex);
        if (error.IsFailure)
        {
            return;
        }

        var pipelineKey = RHIUtility.CreateGraphicsPipelineKey(compiledHash, materialPipeline, passPipelineHash);

        if (!_pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            var compiledCacheResult = _shaderLibrary.GetCompiledCache(shader.UniqueID, passIndex);
            if (compiledCacheResult.IsFailure)
            {
                Logger.Warning($"Failed to load compiled shader cache for graphics pipeline {pipelineKey}. Skipping draw call.");
                return;
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 3); // as, ms, ps

            var asByteCode = byteCodes.Slice((int)byteCodeOffsets[0], (int)(byteCodeOffsets[1] - byteCodeOffsets[0]));
            var msByteCode = byteCodes.Slice((int)byteCodeOffsets[1], (int)(byteCodeOffsets[2] - byteCodeOffsets[1]));
            var psByteCode = byteCodes.Slice((int)byteCodeOffsets[2]);

            var psoDes = new GraphicsPSODesc
            {
                CompiledHash = compiledHash,
                PipelineOption = materialPipeline,
                RtvFormats = _rtvFormats.AsSpan(0, _rtvCount),
                DsvFormat = _dsvFormat,

                AsCode = asByteCode,
                MsCode = msByteCode,
                PsCode = psByteCode,
            };

            _pipelineLibrary.CreateGraphicsPipeline(in psoDes).GetValueOrThrow();
        }

        _activePerMaterialData = material._cBufferCache.GpuResource;
        _commandBuffer.SetPipelineState(pipelineKey);
    }

    public void SetActiveShaderPass(Handle<Shader> shaderHandle, PassSemantic semantic)
    {
        var r = _resourceManager.GetShaderReference(shaderHandle);
        if (r.IsFailure)
        {
            throw InvalidResourceHandleException.Create(shaderHandle);
        }

        ref var shader = ref r.Value;
        var passIndex = shader.GetPassIndex(semantic);
        if (passIndex < 0)
        {
            return;
        }

        var (compiledHash, error) = _shaderLibrary.GetCompiledHash(shader.UniqueID, passIndex);
        if (error.IsFailure)
        {
            return;
        }

        var pipelineKey = RHIUtility.CreateComputePipelineKey(compiledHash);

        if (!_pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            var compiledCacheResult = _shaderLibrary.GetCompiledCache(shader.UniqueID, passIndex);
            if (compiledCacheResult.IsFailure)
            {
                Logger.Warning($"Failed to load compiled shader cache for compute pipeline {pipelineKey}. Skipping compute dispatch.");
                return;
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 1);

            var psoDes = new ComputePSODesc
            {
                CompiledHash = compiledHash,
                CsCode = byteCodes.Slice((int)byteCodeOffsets[0]),
            };

            _pipelineLibrary.CreateComputePipeline(in psoDes).GetValueOrThrow();
        }

        _commandBuffer.SetPipelineState(pipelineKey);
    }


    public void SetActiveMesh(Handle<Mesh> mesh)
    {
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            throw InvalidResourceHandleException.Create(mesh);
        }

        ref readonly var meshRef = ref r.Value;
        SetActiveMesh(in meshRef);
    }

    public void SetActiveMesh(scoped in Mesh mesh)
    {
        _activePerMeshData = mesh.MeshDataBuffer;
        _activeMeshIndexCount = mesh.IndexCount;
    }

    public void SetGlobalData(uint frameBuffer, uint viewBuffer)
    {
        _activeFrameBuffer = frameBuffer;
        _activeViewBuffer = viewBuffer;
    }

    public void SetInstanceIndex(uint instanceIndex)
    {
        _activeInstanceIndex = instanceIndex;
    }

    public unsafe void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        var data = new PushConstantsData
        {
            frameBuffer = _activeFrameBuffer,
            viewBuffer = _activeViewBuffer,
            instanceIndex = _activeInstanceIndex,
        };

        var pushConstantSpan = new ReadOnlySpan<uint>(&data, sizeof(PushConstantsData) / sizeof(uint));
        _commandBuffer.SetGraphicsRoot32Constants(RootSignatureLayout.PUSH_CONSTANT_SLOT, pushConstantSpan);
        _commandBuffer.DispatchMesh(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void SetActiveCompute(Handle<ComputeShader> computeShader, int entryIndex)
    {
        var r = _resourceManager.GetComputeShaderReference(computeShader);
        if (r.IsFailure)
        {
            throw InvalidResourceHandleException.Create(computeShader);
        }

        ref var shader = ref r.Value;

        var (compiledHash, error) = _shaderLibrary.GetCompiledHash(shader.UniqueID, entryIndex);
        if (error.IsFailure)
        {
            // TODO: Fallback to a default shader or show an error material.
            return;
        }

        var pipelineKey = RHIUtility.CreateComputePipelineKey(compiledHash);

        if (!_pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            var compiledCacheResult = _shaderLibrary.GetCompiledCache(shader.UniqueID, entryIndex);
            if (compiledCacheResult.IsFailure)
            {
                Logger.Warning($"Failed to load compiled shader cache for compute pipeline {pipelineKey}. Skipping compute dispatch.");
                return;
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 1);

            var psoDes = new ComputePSODesc
            {
                CompiledHash = compiledHash,

                CsCode = byteCodes.Slice((int)byteCodeOffsets[0]),
            };

            _pipelineLibrary.CreateComputePipeline(in psoDes).GetValueOrThrow();
        }

        _commandBuffer.SetPipelineState(pipelineKey);
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        _commandBuffer.DispatchCompute(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public ICommandBuffer GetCommandBufferUnsafe()
    {
        return _commandBuffer;
    }
}
