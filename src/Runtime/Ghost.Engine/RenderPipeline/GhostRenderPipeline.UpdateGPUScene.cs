using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;


namespace Ghost.Engine.RenderPipeline;

[GenerateShaderProperty("Internal/UpdateGPUScene")]
public partial struct UpdateGPUSceneShaderProperty
{
    public uint gpuSceneBuffer;
    public uint addBuffer;
    public uint addCount;
    public uint removeBuffer;
    public uint removeCount;
}

internal partial class GhostRenderPipeline
{
    private static unsafe Handle<GPUBuffer> CreateAddInstanceBuffer(GhostRenderPayload ghostPayload, ResourceManager resourceManager, IResourceDatabase resourceDatabase, out int count)
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
                    Logger.Error($"Failed to get mesh reference for mesh instance with ID {addRequest.instanceId}");
                    continue;
                }

                pAddData[i] = new AddInstanceData
                {
                    localToWorld = addRequest.localToWorld,
                    instanceID = addRequest.instanceId,
                    meshBuffer = resourceDatabase.GetBindlessIndex(mesh.Get().MeshDataBuffer.AsResource()),
                    materialPalette = (uint)addRequest.meshInstance.materialPalette.Value,
                    renderingLayerMask = addRequest.meshInstance.renderingLayerMask,
                    shadowCastingMode = (uint)addRequest.meshInstance.shadowCastingMode
                };

                i++;
            }

            resourceDatabase.UnmapResource(addBuffer.AsResource(), 0, null);

            count = i;
            return addBuffer;
        }

        count = 0;
        return default;
    }

    private static unsafe Handle<GPUBuffer> CreateRemoveInstanceBuffer(GhostRenderPayload ghostPayload, ResourceManager resourceManager, IResourceDatabase resourceDatabase, out int count)
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
                    instanceID = removeRequest.instanceId,
                    swapWithInstanceID = removeRequest.swapWithInstanceId
                };

                i++;
            }

            resourceDatabase.UnmapResource(removeBuffer.AsResource(), 0, null);

            count = i;
            return removeBuffer;
        }

        count = 0;
        return default;
    }

    public void UpdateGPUScene(RenderContext ctx, GhostRenderPayload payload)
    {
        var addBuffer = CreateAddInstanceBuffer(payload, ctx.ResourceManager, ctx.ResourceDatabase, out var addCount);
        var removeBuffer = CreateRemoveInstanceBuffer(payload, ctx.ResourceManager, ctx.ResourceDatabase, out var removeCount);

        if (addCount <= 0 && removeCount <= 0)
        {
            Logger.DebugAssert(addBuffer.IsInvalid && removeBuffer.IsInvalid, "Buffers should be invalid when there are no updates.");
            return; // No updates needed
        }

        // NOTE: We dispatch it here instead of in render graph is because the update does not perform every frame.
        // The topology change of the graph will trigger the recompilation of the render graph, which is expensive.
        // Currently the render graph does not support import invalid resources, which means we can not handle the early return in the render func.
        // Furthermore, updating the GPU scene does not rely on other resources and passes, it's isolated and always run before the actual rendering.
        // So it's fine to dispatch it here directly.

        var property = new UpdateGPUSceneShaderProperty
        {
            gpuSceneBuffer = ctx.ResourceDatabase.GetBindlessIndex(_gpuScene.SceneBuffer.AsResource(), BindlessAccess.UnorderedAccess),
            addBuffer = ctx.ResourceDatabase.GetBindlessIndex(addBuffer.AsResource()),
            addCount = (uint)addCount,
            removeBuffer = ctx.ResourceDatabase.GetBindlessIndex(removeBuffer.AsResource()),
            removeCount = (uint)removeCount
        };

        // TODO: Write and load the shader. This is just a placeholder for now.
        var shader = Handle<ComputeShader>.Invalid;
        var keywords = new LocalKeywordSet();

        ctx.DispatchCompute(shader, 0, in keywords, in property, new uint3());
    }
}
