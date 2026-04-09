using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
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

    ICommandBuffer GetCommandBufferUnsafe();
}

public interface IRasterRenderContext : IRenderGraphContext
{
    void SetViewport(ViewportDesc desc);
    void SetScissorRect(ScissorRectDesc desc);

    void SetGlobalData(uint globalIndex, uint viewIndex);
    void SetInstanceIndex(uint instanceIndex);

    void SetActiveMaterial(Handle<Material> material);
    void SetActiveMaterial(ref readonly Material material);
    void SetActiveMesh(Handle<Mesh> mesh);
    void SetActiveMesh(ref readonly Mesh mesh);
    void DispatchMesh(uint3 threadGroupCount);
}

public interface IComputeRenderContext : IRenderGraphContext
{
    void DispatchCompute(uint3 threadGroupCount);
}

public interface IUnsafeRenderContext : IRasterRenderContext, IComputeRenderContext
{
}

internal sealed class RenderGraphContext : IUnsafeRenderContext
{
    private readonly ResourceManager _resourceManager;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly IShaderCompiler _shaderCompiler;
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

    internal RenderGraphContext(ResourceManager resourceManager, IResourceDatabase resourceDatabase, IPipelineLibrary pipelineLibrary, IShaderCompiler shaderCompiler, RenderGraphResourceRegistry resources)
    {
        _resourceManager = resourceManager;
        _resourceDatabase = resourceDatabase;
        _pipelineLibrary = pipelineLibrary;
        _shaderCompiler = shaderCompiler;
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
            _activePerMaterialData = Handle<GPUBuffer>.Invalid;
            return;
        }

        ref readonly var mat = ref r.Value;
        SetActiveMaterial(in mat);
    }

    public void SetActiveMaterial(ref readonly Material material)
    {
        var shaderResult = _resourceManager.GetShaderReference(material.Shader);
        if (shaderResult.IsFailure)
        {
            _activePerMaterialData = Handle<GPUBuffer>.Invalid;
            return;
        }

        ref readonly var shader = ref shaderResult.Value;
        ref readonly var pass = ref shader.GetPassReference(material.ActivePassIndex);

        var passPipelineHash = new PassPipelineHash(_rtvFormats, _dsvFormat);
        var materialPipeline = material.GetPassPipelineOverride(material.ActivePassIndex);

        // Mask out the keywords that are not used in this pass.
        var variantMask = material._keywordMask & pass.KeywordIDs;
        var shaderVariantKey = RHIUtility.CreateShaderVariantKey(pass.Key, in variantMask);
        var pipelineKey = RHIUtility.CreateGraphicsPipelineKey(shaderVariantKey, materialPipeline, passPipelineHash);

        if (!_pipelineLibrary.HasPipeline(pipelineKey))
        {
            var compiledCacheResult = _shaderCompiler.LoadCompiledCache(shaderVariantKey);
            if (compiledCacheResult.IsFailure)
            {
                throw new InvalidOperationException("Failed to load compiled shader cache for pipeline state object creation.");
            }

            var psoDes = new GraphicsPSODescriptor
            {
                VariantKey = shaderVariantKey,
                PipelineOption = materialPipeline,

                RtvFormats = _rtvFormats.AsSpan(0, _rtvCount),
                DsvFormat = _dsvFormat,
            };

            var compiled = compiledCacheResult.Value;
            _pipelineLibrary.CreateGraphicsPipeline(in psoDes, in compiled).GetValueOrThrow();
        }

        _activePerMaterialData = material._cBufferCache.GpuResource;
        _commandBuffer.SetPipelineState(pipelineKey);
    }

    public void SetActiveMesh(Handle<Mesh> mesh)
    {
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            _activePerMeshData = Handle<GPUBuffer>.Invalid;
            _activeMeshIndexCount = 0;
            return;
        }

        ref readonly var meshRef = ref r.Value;
        SetActiveMesh(in meshRef);
    }

    public void SetActiveMesh(ref readonly Mesh mesh)
    {
        _activePerMeshData = mesh.ObjectDataBuffer;
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

    public unsafe void DispatchMesh(uint3 threadGroupCount)
    {
        var data = new PushConstantsData
        {
            frameBuffer = _activeFrameBuffer,
            viewBuffer = _activeViewBuffer,
            instanceIndex = _activeInstanceIndex,
        };

        var pushConstantSpan = new ReadOnlySpan<uint>(&data, sizeof(PushConstantsData) / sizeof(uint));
        _commandBuffer.SetGraphicsRoot32Constants(RootSignatureLayout.PUSH_CONSTANT_SLOT, pushConstantSpan);
        _commandBuffer.DispatchMesh(threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
    }

    public void DispatchCompute(uint3 threadGroupCount)
    {
        throw new NotImplementedException();
    }


    public ICommandBuffer GetCommandBufferUnsafe()
    {
        return _commandBuffer;
    }
}
