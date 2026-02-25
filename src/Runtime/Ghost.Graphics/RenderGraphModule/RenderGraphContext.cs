using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.RenderGraphModule;

public interface IRenderGraphContext
{
    IResourceManager ResourceManager { get; }

    Handle<GPUResource> GetActualResource(Identifier<RGResource> resource);
    Handle<Texture> GetActualTexture(Identifier<RGTexture> texture);
    Handle<GraphicsBuffer> GetActualBuffer(Identifier<RGBuffer> buffer);
}

public interface IRasterRenderContext : IRenderGraphContext
{
    int ActiveMeshIndexCount { get; }

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

public interface IUnsafeRenderContext : IRasterRenderContext, IRenderGraphContext
{
    ICommandBuffer CommandBuffer { get; }
}

internal sealed class RenderGraphContext : IRasterRenderContext, IComputeRenderContext, IUnsafeRenderContext
{
    private readonly IResourceManager _resourceManager;
    private readonly IPipelineLibrary _pipelineLibrary;
    private readonly IShaderCompiler _shaderCompiler;
    private readonly RenderGraphResourceRegistry _resources;

    private ICommandBuffer _commandBuffer = null!;

    private readonly TextureFormat[] _rtvFormats;
    private TextureFormat _dsvFormat;
    private int _rtvCount;

    private Handle<GraphicsBuffer> _activePerMaterialData;
    private Handle<GraphicsBuffer> _activePerMeshData;
    private int _activeMeshIndexCount;

    public IResourceManager ResourceManager => _resourceManager;

    public int ActiveMeshIndexCount => _activeMeshIndexCount;

    public ICommandBuffer CommandBuffer => _commandBuffer;

    internal RenderGraphContext(IResourceManager resourceManager, IPipelineLibrary pipelineLibrary, IShaderCompiler shaderCompiler, RenderGraphResourceRegistry resources)
    {
        _resourceManager = resourceManager;
        _pipelineLibrary = pipelineLibrary;
        _shaderCompiler = shaderCompiler;
        _resources = resources;

        _rtvFormats = new TextureFormat[RHIUtility.MAX_RENDER_TARGETS];
        _dsvFormat = TextureFormat.Unknown;
    }

    internal void SetCommandBuffer(ICommandBuffer commandBuffer)
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
        return _resources.GetResource(resource).backingResource;
    }

    public Handle<Texture> GetActualTexture(Identifier<RGTexture> texture)
    {
        return _resources.GetResource(texture.AsResource()).backingResource.AsTexture();
    }

    public Handle<GraphicsBuffer> GetActualBuffer(Identifier<RGBuffer> buffer)
    {
        return _resources.GetResource(buffer.AsResource()).backingResource.AsGraphicsBuffer();
    }

    public void SetActiveMaterial(Handle<Material> material)
    {
        var r = _resourceManager.GetMaterialReference(material);
        if (r.IsFailure)
        {
            _activePerMaterialData = Handle<GraphicsBuffer>.Invalid;
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
            _activePerMaterialData = Handle<GraphicsBuffer>.Invalid;
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
            _pipelineLibrary.CompilePSO(in psoDes, in compiled).GetValueOrThrow();
        }

        _activePerMaterialData = material._cBufferCache.GpuResource;
        _commandBuffer.SetPipelineState(pipelineKey);
    }

    public void SetActiveMesh(Handle<Mesh> mesh)
    {
        var r = _resourceManager.GetMeshReference(mesh);
        if (r.IsFailure)
        {
            _activePerMeshData = Handle<GraphicsBuffer>.Invalid;
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

    public unsafe void DispatchMesh(uint3 threadGroupCount)
    {
        // TODO: Global and view constants
        var data = new PushConstantsData
        {
            objectIndex = _resourceManager.ResourceDatabase.GetBindlessIndex(_activePerMeshData.AsResource()),
            materialIndex = _resourceManager.ResourceDatabase.GetBindlessIndex(_activePerMaterialData.AsResource()),
        };

        var pushConstantSpan = new ReadOnlySpan<uint>(&data, sizeof(PushConstantsData) / sizeof(uint));
        _commandBuffer.SetGraphicsRoot32Constants(RootSignatureLayout.PUSH_CONSTANT_SLOT, pushConstantSpan);
        _commandBuffer.DispatchMesh(threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
    }

    public void DispatchCompute(uint3 threadGroupCount)
    {
        throw new NotImplementedException();
    }
}