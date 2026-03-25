using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RenderPipeline;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Graphics.Test.RenderPasses;

public sealed class TestRenderPipelineSettings : IRenderPipelineSettings
{
    public IRenderPipeline CreatePipeline(RenderSystem renderSystem)
    {
        return new TestRenderPipeline(renderSystem);
    }
}

public unsafe partial class TestRenderPipeline : IRenderPipeline
{
    private class MeshletDebugPassData
    {
        public Identifier<RGTexture> backbuffer;
        public RenderList renderList;
        public Handle<Material> material;
        public uint globalIndex;
        public uint viewIndex;
    }

    private readonly RenderGraph _renderGraph;
    private readonly RenderSystem _renderSystem;
    private Identifier<Shader> _meshletShader;
    private Handle<Material> _meshletMaterial;

    private bool _disposed;

    ~TestRenderPipeline()
    {
        Dispose();
    }

    internal TestRenderPipeline(RenderSystem renderSystem)
    {
        _renderSystem = renderSystem;
        _renderGraph = new RenderGraph(renderSystem.ResourceManager,
                renderSystem.GraphicsEngine.ResourceAllocator,
                renderSystem.GraphicsEngine.ResourceDatabase,
                renderSystem.GraphicsEngine.PipelineLibrary,
                renderSystem.GraphicsEngine.ShaderCompiler);

        var shaderDescriptor = DSLShaderCompiler.CompileShader("F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/test.gshdr", "C:/Users/Misaki/Downloads/Archive").GetValueOrThrow();
        _meshletShader = renderSystem.ResourceManager.CreateGraphicsShader(shaderDescriptor);
        _meshletMaterial = renderSystem.ResourceManager.CreateMaterial(_meshletShader);

        var config = new ShaderCompilationConfig
        {
            optimizeLevel = CompilerOptimizeLevel.O3,
            options = CompilerOption.KeepReflections,
            tier = CompilerTier.Tier2
        };

        var pass = shaderDescriptor.passes[0];
        var emptyKeywords = new LocalKeywordSet();
        var variantKey = RHIUtility.CreateShaderVariantKey(
            RHIUtility.CreateShaderPassKey(pass.identifier),
            in emptyKeywords);

        renderSystem.GraphicsEngine.ShaderCompiler.CompilePass(in pass, in config, variantKey).GetValueOrThrow();
    }

    public void Render(RenderContext ctx, ReadOnlySpan<RenderRequest> requests)
    {
        var resourceManager = _renderSystem.ResourceManager;
        var resourceDatabase = _renderSystem.GraphicsEngine.ResourceDatabase;

        for (var i = 0; i < requests.Length; i++)
        {
            ref readonly var request = ref requests[i];

            // 1. Allocate and populate Instance Data buffer
            var instanceCount = request.opaqueRenderList.TotalRecordCount;
            if (instanceCount == 0)
            {
                continue; // Nothing to render
            }

            var instanceDataSize = (uint)(instanceCount * sizeof(InstanceData));
            var instanceBufferDesc = ResourceDesc.Buffer(new BufferDesc
            {
                Size = instanceDataSize,
                Stride = (uint)sizeof(InstanceData),
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                MemoryType = ResourceMemoryType.Upload, // Upload directly for simplicity in testing
            });

            // TODO: Optimize by suballocation.
            var instanceBufferHandle = resourceManager.GetPooledResource(instanceBufferDesc);
            var instanceBufferResource = instanceBufferHandle.AsGraphicsBuffer();

            var instanceDataArray = new InstanceData[instanceCount];
            var instanceIdx = 0;
            foreach (var record in request.opaqueRenderList)
            {
                instanceDataArray[instanceIdx++] = new InstanceData
                {
                    localToWorld = record.localToWorld
                };
            }

            ctx.CommandBuffer.UploadBuffer(instanceBufferResource, instanceDataArray.AsSpan());

            // 2. Allocate and populate View Data buffer
            var viewDataSize = (uint)sizeof(PerViewData);
            var viewBufferDesc = ResourceDesc.Buffer(new BufferDesc
            {
                Size = viewDataSize,
                Stride = viewDataSize,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                MemoryType = ResourceMemoryType.Upload,
            });

            var viewBufferHandle = resourceManager.GetPooledResource(viewBufferDesc);
            var viewBufferResource = viewBufferHandle.AsGraphicsBuffer();

            var viewData = new PerViewData
            {
                viewMatrix = request.view.viewMatrix,
                projectionMatrix = request.view.projectionMatrix,
                cameraPosition = request.view.position,
                nearClip = request.view.nearClipPlane,
                cameraDirection = request.view.viewMatrix.c2.xyz, // check if that's correct orientation
                farClip = request.view.farClipPlane,
                screenSize = new float4(request.view.sensorSize.x, request.view.sensorSize.y, 1.0f / request.view.sensorSize.x, 1.0f / request.view.sensorSize.y)
            };

            ctx.CommandBuffer.UploadBuffer(viewBufferResource, new ReadOnlySpan<PerViewData>(in viewData));

            // 3. Allocate and populate Global Frame Data buffer
            var frameDataSize = (uint)sizeof(GlobalFrameData);
            var frameBufferDesc = ResourceDesc.Buffer(new BufferDesc
            {
                Size = frameDataSize,
                Stride = frameDataSize,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource, // or CBV? Let's use Raw to keep it consistent
                MemoryType = ResourceMemoryType.Upload,
            });

            var frameBufferHandle = resourceManager.GetPooledResource(frameBufferDesc);
            var frameBufferResource = frameBufferHandle.AsGraphicsBuffer();

            var frameData = new GlobalFrameData
            {
                viewBufferIndex = resourceDatabase.GetBindlessIndex(viewBufferResource.AsResource()),
                instanceBufferIndex = resourceDatabase.GetBindlessIndex(instanceBufferResource.AsResource()),
            };

            ctx.CommandBuffer.UploadBuffer(frameBufferResource, new ReadOnlySpan<GlobalFrameData>(in frameData));

            if (request.renderFunc != null)
            {
                request.renderFunc(in ctx, in request);
            }
            else
            {
                var backBuffer = _renderGraph.ImportTexture(request.colorTarget, "BackBuffer", clearAtFirstUse: true, discardAtLastUse: false);

                MeshletDebugPass(backBuffer, request.opaqueRenderList, resourceDatabase.GetBindlessIndex(frameBufferResource.AsResource()), resourceDatabase.GetBindlessIndex(viewBufferResource.AsResource()));
            }

            // We must enqueue a return for the pooled resources so they are freed next frame.
            resourceManager.ReturnPooledResource(instanceBufferHandle);
            resourceManager.ReturnPooledResource(viewBufferHandle);
            resourceManager.ReturnPooledResource(frameBufferHandle);
        }
    }

    private void MeshletDebugPass(Identifier<RGTexture> backbuffer, RenderList renderList, uint globalIndex, uint viewIndex)
    {
        using (var builder = _renderGraph.AddRasterRenderPass<MeshletDebugPassData>("Meshlet Debug Pass", out var passData))
        {
            passData.renderList = renderList;
            passData.globalIndex = globalIndex;
            passData.viewIndex = viewIndex;
            passData.material = _meshletMaterial;

            builder.SetColorAttachment(backbuffer, 0);
            builder.SetRenderFunc<MeshletDebugPassData>(static (data, ctx)=>
            {
                ctx.SetGlobalData(data.globalIndex, data.viewIndex);
                ctx.SetActiveMaterial(data.material);

                var instanceIndex = 0u;
                foreach (var record in data.renderList)
                {
                    ctx.SetActiveMesh(record.mesh);
                    ctx.SetInstanceIndex(instanceIndex);

                    var meshRefResult = ctx.ResourceManager.GetMeshReference(record.mesh);
                    if (meshRefResult.IsSuccess)
                    {
                        var meshletCount = (uint)meshRefResult.Value.MeshletData.meshlets.Count;
                        ctx.DispatchMesh(new uint3(meshletCount, 1, 1));
                    }
                    instanceIndex++;
                }
            });
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _renderSystem.ResourceManager.ReleaseMaterial(_meshletMaterial);
        _renderSystem.ResourceManager.ReleaseShader(_meshletShader);

        _renderGraph.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
