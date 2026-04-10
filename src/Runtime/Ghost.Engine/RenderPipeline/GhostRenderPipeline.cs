using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics;

namespace Ghost.Engine.RenderPipeline;

internal class GhostRenderPipeline : IRenderPipeline
{
    private struct AddInstanceData
    {
        public float4x4 localToWorld;
        public uint instanceId;
        public uint meshBuffer;
        public uint materialPalette;
        public uint renderingLayerMask;
        public uint shadowCastingMode;
    }

    private struct RemoveInstanceData
    {
        public uint instanceId;
        public uint swapWithInstanceId;
    }

    private readonly RenderSystem _renderSystem;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;

    public GPUScene GPUScene => _gpuScene;

    public GhostRenderPipeline(RenderSystem renderSystem)
    {
        _renderSystem = renderSystem;

        _renderGraph = new RenderGraph(renderSystem.ResourceManager, renderSystem.GraphicsEngine);
        _gpuScene = new GPUScene(renderSystem.GraphicsEngine.ResourceAllocator, renderSystem.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
    }

    private static unsafe Handle<GPUBuffer> CreateAddInstanceBuffer(GhostRenderPayload ghostPayload, ResourceManager resourceManager, IResourceDatabase resourceDatabase)
    {
        if (!ghostPayload.AddRequest.IsEmpty)
        {
            var addDesc = new BufferDesc
            {
                Size = (nuint)ghostPayload.AddRequest.Count * MemoryUtility.SizeOf<AddInstanceData>(),
                Stride = (uint)MemoryUtility.SizeOf<AddInstanceData>(),
                Usage = BufferUsage.Structured | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var addBuffer = resourceManager.CreateTransientBuffer(in addDesc, "Add Instance Buffer");
            var pAddData = (AddInstanceData*)resourceDatabase.MapResource(addBuffer.AsResource(), 0, null);

            var i = 0;
            while (ghostPayload.AddRequest.TryDequeue(out var addRequest))
            {
                var (mesh, error) = resourceManager.GetMeshReference(addRequest.meshInstance.mesh);
                if (error.IsFailure)
                {
                    Debug.Fail($"Failed to get mesh reference for mesh instance with ID {addRequest.instanceId}");
                    continue;
                }

                pAddData[i] = new AddInstanceData
                {
                    localToWorld = addRequest.localToWorld,
                    instanceId = addRequest.instanceId,
                    meshBuffer = resourceDatabase.GetBindlessIndex(mesh.Get().MeshDataBuffer.AsResource()),
                    materialPalette = (uint)addRequest.meshInstance.materialPalette.Value,
                    renderingLayerMask = addRequest.meshInstance.renderingLayerMask,
                    shadowCastingMode = (uint)addRequest.meshInstance.shadowCastingMode
                };

                i++;
            }

            resourceDatabase.UnmapResource(addBuffer.AsResource(), 0, null);
            return addBuffer;
        }

        return default;
    }

    private static unsafe Handle<GPUBuffer> CreateRemoveInstanceBuffer(GhostRenderPayload ghostPayload, ResourceManager resourceManager, IResourceDatabase resourceDatabase)
    {
        if (!ghostPayload.RemoveRequest.IsEmpty)
        {
            var addDesc = new BufferDesc
            {
                Size = (nuint)ghostPayload.AddRequest.Count * MemoryUtility.SizeOf<RemoveInstanceData>(),
                Stride = (uint)MemoryUtility.SizeOf<RemoveInstanceData>(),
                Usage = BufferUsage.Structured | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var removeBuffer = resourceManager.CreateTransientBuffer(in addDesc, "Remove Instance Buffer");
            var pRemoveData = (RemoveInstanceData*)resourceDatabase.MapResource(removeBuffer.AsResource(), 0, null);

            var i = 0;
            while (ghostPayload.RemoveRequest.TryDequeue(out var removeRequest))
            {
                pRemoveData[i] = new RemoveInstanceData
                {
                    instanceId = removeRequest.instanceId,
                    swapWithInstanceId = removeRequest.swapWithInstanceId
                };

                i++;
            }

            resourceDatabase.UnmapResource(removeBuffer.AsResource(), 0, null);
            return removeBuffer;
        }

        return default;
    }

    public void Render(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        var resourceManager = _renderSystem.ResourceManager;
        var resourceDatabase = _renderSystem.GraphicsEngine.ResourceDatabase;

        foreach (ref readonly var request in ghostPayload.RenderRequests)
        {
            if (!RenderPipelineUtility.GetVPMatrices(_renderSystem, in request, out var view, out var projection, out var screenSize))
            {
                continue;
            }

            var addBuffer = CreateAddInstanceBuffer(ghostPayload, resourceManager, resourceDatabase);
            var removeBuffer = CreateRemoveInstanceBuffer(ghostPayload, resourceManager, resourceDatabase);


        }
    }

    public void Dispose()
    {
        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
