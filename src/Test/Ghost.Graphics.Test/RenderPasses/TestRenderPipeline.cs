using Ghost.Core;
using Ghost.DSL.ShaderCompiler;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RenderPipeline;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
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
        public RenderList renderList;
        public Handle<Material> material;
        public uint globalIndex;
        public uint viewIndex;
        public uint instanceIndex;
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

    private static float3 IntersectFrustumPlanes(float4 p0, float4 p1, float4 p2)
    {
        var n0 = p0.xyz;
        var n1 = p1.xyz;
        var n2 = p2.xyz;

        float det = math.dot(math.cross(n0, n1), n2);
        return (math.cross(n2, n1) * p0.w + math.cross(n0, n2) * p1.w - math.cross(n0, n1) * p2.w) * (1.0f / det);
    }

    private static Frustum CreateFrustum(float nearClip, float farClip, float4x4 vp, float3 viewDir, float3 viewPos)
    {
        var frustum = new Frustum();
        Frustum.CalculateFrustumPlanes(vp, ref frustum.planes);

        // We need to recalculate the near and far planes otherwise it does not work for oblique projection matrices used for reflection.
        var nearPlane = Plane.CreateFromUnitNormalAndPointInPlane(viewDir, viewPos);
        nearPlane.Distance -= nearClip;

        var farPlane = Plane.CreateFromUnitNormalAndPointInPlane(-viewDir, viewPos);
        farPlane.Distance += farClip;

        frustum.planes[4] = nearPlane;
        frustum.planes[5] = farPlane;

        frustum.corners[0] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[4]);
        frustum.corners[1] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[4]);
        frustum.corners[2] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[4]);
        frustum.corners[3] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[4]);
        frustum.corners[4] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[5]);
        frustum.corners[5] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[5]);
        frustum.corners[6] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[5]);
        frustum.corners[7] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[5]);
        return frustum;
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

            Handle<GPUTexture> rt;
            if (request.swapChainIndex < 0)
            {
                rt = request.colorTarget;
            }
            else if (_renderSystem.SwapChainManager.TryGetSwapChain(request.swapChainIndex, out var swapChain))
            {
                rt = swapChain.GetCurrentBackBuffer();
            }
            else
            {
                continue;
            }

            try
            {
                var rtResult = _renderSystem.GraphicsEngine.ResourceDatabase.GetResourceDescription(rt.AsResource());
                if (rtResult.IsFailure)
                {
                    continue;
                }

                var rtSize = new uint2(rtResult.Value.TextureDescription.Width, rtResult.Value.TextureDescription.Height);
                var aspectScreen = (float)rtSize.x / rtSize.y;


                // NOTE: We assume camera's scale is always (1, 1, 1). Otherwise fastinverse will fail and we need to use regular inverse which is more expensive.
                var viewMatrix = math.fastinverse(request.view.localToWorld);

                var vfov = 2.0f * math.atan(request.view.sensorSize.y / (2.0f * request.view.focalLength));
                var hfov = 2.0f * math.atan(request.view.sensorSize.x / (2.0f * request.view.focalLength));
                var aspectSensor = request.view.sensorSize.x / request.view.sensorSize.y;

                float vfovF;
                switch (request.view.gateFit)
                {
                    case GateFit.Vertical:
                        vfovF = vfov;
                        break;

                    case GateFit.Horizontal:
                        // Adjust VFOV so that the sensor width fits the screen width
                        var horizontalAspectBuffer = math.tan(hfov * 0.5f);
                        vfovF = 2.0f * math.atan(horizontalAspectBuffer / aspectScreen);
                        break;

                    case GateFit.Fill:
                        if (aspectSensor > aspectScreen)
                        {
                            goto case GateFit.Vertical;
                        }
                        else
                        {
                            goto case GateFit.Horizontal;
                        }

                    case GateFit.Overscan:
                        if (aspectSensor > aspectScreen)
                        {
                            goto case GateFit.Horizontal;
                        }
                        else
                        {
                            goto case GateFit.Vertical;
                        }
                    default:
                        vfovF = vfov;
                        break;
                }

                var m_11 = 1.0f / math.tan(vfovF * 0.5f);
                var m_00 = m_11 / aspectScreen;
                var m_22 = request.view.farClipPlane / (request.view.farClipPlane - request.view.nearClipPlane);
                var m_23 = -(request.view.farClipPlane * request.view.nearClipPlane) / (request.view.farClipPlane - request.view.nearClipPlane);

                var projectionMatrix = new float4x4
                (
                    m_00, 0, 0, 0,
                    0, m_11, 0, 0,
                    0, 0, m_22, m_23,
                    0, 0, 1, 0
                );

                //var vp = math.mul(projectionMatrix, viewMatrix);
                //var viewDir = math.normalize(request.view.localToWorld.c2.xyz);
                //var viewPos = request.view.localToWorld.c3.xyz;
                //var frustum = CreateFrustum(request.view.nearClipPlane, request.view.farClipPlane, vp, viewDir, viewPos);

                var instanceDataSize = (uint)(instanceCount * sizeof(InstanceData));
                var instanceBufferDesc = new BufferDesc
                {
                    Size = instanceDataSize,
                    Stride = (uint)sizeof(InstanceData),
                    Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                    HeapType = HeapType.Upload, // Upload directly for simplicity in testing
                };

                var instanceBufferHandle = resourceManager.CreateTransientBuffer(in instanceBufferDesc, "Instance Buffer");
                var instanceBufferResource = instanceBufferHandle.AsResource();

                var instanceDataArray = new InstanceData[instanceCount];
                var instanceIdx = 0;
                foreach (var record in request.opaqueRenderList)
                {
                    var (mesh, error) = resourceManager.GetMeshReference(record.mesh);
                    if (error.IsFailure)
                    {
                        continue;
                    }

                    (var mat, error) = resourceManager.GetMaterialReference(_meshletMaterial);
                    if (error.IsFailure)
                    {
                        continue;
                    }

                    instanceDataArray[instanceIdx++] = new InstanceData
                    {
                        localToWorld = record.localToWorld,
                        meshBuffer = resourceDatabase.GetBindlessIndex(mesh.Get().ObjectDataBuffer.AsResource()),
                        materialBuffer = resourceDatabase.GetBindlessIndex(mat.Get()._cBufferCache.GpuResource.AsResource())
                    };
                }

                ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(instanceBufferResource, BarrierSync.Copy, BarrierAccess.CopyDest));
                ctx.UploadBuffer(instanceBufferHandle, instanceDataArray.AsSpan());
                ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(instanceBufferResource, BarrierSync.AllShading, BarrierAccess.ShaderResource));

                // 2. Allocate and populate View Data buffer
                var viewBufferDesc = new BufferDesc
                {
                    Size = (uint)sizeof(ViewData),
                    Stride = (uint)sizeof(ViewData),
                    Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                    HeapType = HeapType.Upload,
                };

                var viewBufferHandle = resourceManager.CreateTransientBuffer(in viewBufferDesc, "View Buffer");
                var viewBufferResource = viewBufferHandle.AsResource();

                var viewData = new ViewData
                {
                    viewMatrix = viewMatrix,
                    projectionMatrix = projectionMatrix,
                    cameraPosition = request.view.localToWorld.c3.xyz,
                    nearClip = request.view.nearClipPlane,
                    cameraDirection = viewMatrix.c2.xyz, // check if that's correct orientation
                    farClip = request.view.farClipPlane,
                    screenSize = new float4(request.view.sensorSize.x, request.view.sensorSize.y, 1.0f / request.view.sensorSize.x, 1.0f / request.view.sensorSize.y)
                };

                ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(viewBufferResource, BarrierSync.Copy, BarrierAccess.CopyDest));
                ctx.UploadBuffer(viewBufferHandle, new ReadOnlySpan<ViewData>(in viewData));
                ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(viewBufferResource, BarrierSync.AllShading, BarrierAccess.ShaderResource));

                // 3. Allocate and populate Global Frame Data buffer
                var frameDataSize = (uint)sizeof(FrameData);
                var frameBufferDesc = new BufferDesc
                {
                    Size = frameDataSize,
                    Stride = frameDataSize,
                    Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                    HeapType = HeapType.Upload,
                };

                //var frameBufferHandle = resourceManager.CreateTransientBuffer(in frameBufferDesc, "Frame Buffer");
                //var frameBufferResource = frameBufferHandle.AsResource();

                //var frameData = new FrameData();

                //ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(frameBufferResource, BarrierSync.Copy, BarrierAccess.CopyDest));
                //ctx.UploadBuffer(frameBufferHandle, new ReadOnlySpan<FrameData>(in frameData));
                //ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(frameBufferResource, BarrierSync.AllShading, BarrierAccess.ShaderResource));

                if (request.renderFunc != null)
                {
                    request.renderFunc(in ctx, in request);
                }
                else
                {
                    _renderGraph.Reset();

                    var backBuffer = _renderGraph.ImportTexture(rt, "BackBuffer");

                    MeshletDebugPass(backBuffer, request.opaqueRenderList,
                        uint.MaxValue,
                        resourceDatabase.GetBindlessIndex(viewBufferResource),
                        resourceDatabase.GetBindlessIndex(instanceBufferResource));

                    var viewState = new ViewState(rtSize.x, rtSize.y, rtSize.x, rtSize.y);
                    _renderGraph.Compile(viewState);
                    _renderGraph.Execute(ctx.CommandBuffer);
                }
            }
            finally
            {
                if (request.swapChainIndex >= 0)
                {
                    _renderSystem.SwapChainManager.ReleaseSwapChain(request.swapChainIndex);
                }
            }
        }
    }

    private void MeshletDebugPass(Identifier<RGTexture> backbuffer, RenderList renderList, uint globalIndex, uint viewIndex, uint instanceBuffer)
    {
        using (var builder = _renderGraph.AddRasterRenderPass<MeshletDebugPassData>("Meshlet Debug Pass", out var passData))
        {
            var depth = builder.CreateTexture(RGTextureDesc.RelativeDepth(1.0f), "Depth Texture");

            passData.renderList = renderList;
            passData.globalIndex = globalIndex;
            passData.viewIndex = viewIndex;
            passData.instanceIndex = instanceBuffer;
            passData.material = _meshletMaterial;

            builder.SetColorAttachment(backbuffer, 0);
            builder.SetDepthAttachment(depth);

            builder.SetRenderFunc<MeshletDebugPassData>(static (data, ctx) =>
            {
                ctx.SetGlobalData(data.globalIndex, data.viewIndex);
                ctx.SetInstanceData(data.instanceIndex);
                ctx.SetActiveMaterial(data.material);

                var instanceIndex = 0u;
                foreach (var record in data.renderList)
                {
                    ctx.SetInstanceIndex(instanceIndex);

                    var meshRefResult = ctx.ResourceManager.GetMeshReference(record.mesh);
                    if (meshRefResult.IsSuccess)
                    {
                        var meshletCount = (uint)meshRefResult.Value.MeshletData.meshletCount;
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
