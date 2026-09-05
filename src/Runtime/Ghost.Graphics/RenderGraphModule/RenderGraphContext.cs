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
    bool TrySetActiveShaderPass(Handle<Shader> shader, PassSemantic semantic);
    void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);
    void ExecuteIndirect(ICommandSignature commandSignature, uint maxCommandCount, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset);
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

    private bool TryResolveGraphicsPipeline(scoped in ShaderPass pass, ulong shaderId, int passIndex, PipelineState pipelineOption, out Key128<PipelineState> pipelineKey)
    {
        pipelineKey = default;
        const ShaderStageMask requiredStages = ShaderStageMask.Mesh | ShaderStageMask.Pixel;
        if ((pass.StageMask & ShaderStageMask.Compute) != 0 ||
            (pass.StageMask & requiredStages) != requiredStages)
        {
            Logger.Warning($"Shader pass 0x{pass.Key.Value:X16} does not contain a valid graphics stage topology.");
            return false;
        }

        var (compiledHash, error) = _shaderLibrary.GetCompiledHash(shaderId, passIndex);
        if (error.IsFailure)
        {
            return false;
        }

        var passAttachmentHash = new PassAttachmentHash(_rtvFormats, _dsvFormat);
        pipelineKey = RHIUtility.CreateGraphicsPipelineKey(pass.Key.Value, compiledHash, pipelineOption, passAttachmentHash);
        if (_pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            return true;
        }

        var compiledCacheResult = _shaderLibrary.GetCompiledCache(shaderId, passIndex);
        if (compiledCacheResult.IsFailure)
        {
            Logger.Warning($"Failed to load compiled shader cache for graphics pipeline {pipelineKey}. Skipping draw call.");
            pipelineKey = default;
            return false;
        }

        var cache = compiledCacheResult.Value;
        Logger.DebugAssert(cache.compiledHash == compiledHash);
        ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);

        var hasAmplification = (pass.StageMask & ShaderStageMask.Amplification) != 0;
        var expectedByteCodeCount = hasAmplification ? 3 : 2;
        if (byteCodeOffsets.Length != expectedByteCodeCount)
        {
            Logger.Warning($"Shader pass 0x{pass.Key.Value:X16} has {byteCodeOffsets.Length} bytecode entries, expected {expectedByteCodeCount}. Skipping draw call.");
            pipelineKey = default;
            return false;
        }

        var byteCodeIndex = 0;
        var asByteCode = ReadOnlySpan<byte>.Empty;
        if (hasAmplification)
        {
            asByteCode = byteCodes.Slice((int)byteCodeOffsets[0], (int)(byteCodeOffsets[1] - byteCodeOffsets[0]));
            byteCodeIndex++;
        }

        var msByteCode = byteCodes.Slice((int)byteCodeOffsets[byteCodeIndex], (int)(byteCodeOffsets[byteCodeIndex + 1] - byteCodeOffsets[byteCodeIndex]));
        var psByteCode = byteCodes.Slice((int)byteCodeOffsets[byteCodeIndex + 1]);
        var psoDesc = new GraphicsPSODesc
        {
            CompiledHash = compiledHash,
            PassId = pass.Key.Value,
            PipelineOption = pipelineOption,
            RtvFormats = _rtvFormats.AsSpan(0, _rtvCount),
            DsvFormat = _dsvFormat,
            AsCode = asByteCode,
            MsCode = msByteCode,
            PsCode = psByteCode,
        };

        var createResult = _pipelineLibrary.CreateGraphicsPipeline(in psoDesc);
        if (createResult.IsFailure)
        {
            Logger.Warning($"Failed to create graphics pipeline {pipelineKey}: {createResult.Message}");
            pipelineKey = default;
            return false;
        }

        return true;
    }

    private bool TryResolveComputePipeline(ulong passId, ulong shaderId, int entryIndex, out Key128<PipelineState> pipelineKey)
    {
        pipelineKey = default;
        var (compiledHash, error) = _shaderLibrary.GetCompiledHash(shaderId, entryIndex);
        if (error.IsFailure)
        {
            return false;
        }

        pipelineKey = RHIUtility.CreateComputePipelineKey(passId, compiledHash);
        if (_pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            return true;
        }

        var compiledCacheResult = _shaderLibrary.GetCompiledCache(shaderId, entryIndex);
        if (compiledCacheResult.IsFailure)
        {
            Logger.Warning($"Failed to load compiled shader cache for compute pipeline {pipelineKey}. Skipping compute dispatch.");
            pipelineKey = default;
            return false;
        }

        var cache = compiledCacheResult.Value;
        Logger.DebugAssert(cache.compiledHash == compiledHash);
        ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
        if (byteCodeOffsets.Length != 1)
        {
            Logger.Warning($"Compute shader pass 0x{passId:X16} has {byteCodeOffsets.Length} bytecode entries, expected 1. Skipping compute dispatch.");
            pipelineKey = default;
            return false;
        }

        var psoDesc = new ComputePSODesc
        {
            CompiledHash = compiledHash,
            PassId = passId,
            CsCode = byteCodes.Slice((int)byteCodeOffsets[0]),
        };

        var createResult = _pipelineLibrary.CreateComputePipeline(in psoDesc);
        if (createResult.IsFailure)
        {
            Logger.Warning($"Failed to create compute pipeline {pipelineKey}: {createResult.Message}");
            pipelineKey = default;
            return false;
        }

        return true;
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
        var materialPipeline = material.GetPassPipelineOverride(material.ActivePassIndex);

        if (!TryResolveGraphicsPipeline(in pass, shader.UniqueID, material.ActivePassIndex, materialPipeline, out var pipelineKey))
        {
            return;
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
        var materialPipeline = material.GetPassPipelineOverride(passIndex);
        if (!TryResolveGraphicsPipeline(in pass, shader.UniqueID, passIndex, materialPipeline, out var pipelineKey))
        {
            return;
        }

        _activePerMaterialData = material._cBufferCache.GpuResource;
        _commandBuffer.SetPipelineState(pipelineKey);
    }

    public bool TrySetActiveShaderPass(Handle<Shader> shaderHandle, PassSemantic semantic)
    {
        var r = _resourceManager.GetShaderReference(shaderHandle);
        if (r.IsFailure)
        {
            return false;
        }

        ref var shader = ref r.Value;
        var passIndex = shader.GetPassIndex(semantic);
        if (passIndex < 0)
        {
            return false;
        }

        ref readonly var pass = ref shader.GetPassReference(passIndex);
        if ((pass.StageMask & ShaderStageMask.Compute) == 0)
        {
            return false;
        }

        if (!TryResolveComputePipeline(pass.Key.Value, shader.UniqueID, passIndex, out var pipelineKey))
        {
            return false;
        }

        _commandBuffer.SetPipelineState(pipelineKey);
        return true;
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
        var passId = shader.GetEntryID(entryIndex);
        if (!TryResolveComputePipeline(passId, shader.UniqueID, entryIndex, out var pipelineKey))
        {
            return;
        }

        _commandBuffer.SetPipelineState(pipelineKey);
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        _commandBuffer.DispatchCompute(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void ExecuteIndirect(ICommandSignature commandSignature, uint maxCommandCount, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset)
    {
        _commandBuffer.ExecuteIndirect(commandSignature, maxCommandCount, argumentBuffer, argumentOffset, countBuffer, countBufferOffset);
    }
    public ICommandBuffer GetCommandBufferUnsafe()
    {
        return _commandBuffer;
    }
}
