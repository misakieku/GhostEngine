using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
internal class RemoveGPUInstanceSystem : SystemBase
{
    private RenderSystem _renderSystem = null!;

    private Identifier<EntityQuery> _gpuInstanceQueryID;

    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        _renderSystem = systemAPI.World.GetService<RenderSystem>();

        _gpuInstanceQueryID = QueryBuilder.Create()
            .WithAll<GPUInstanceRef>()
            .WithAbsent<MeshInstance>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_gpuInstanceQueryID);
    }

    protected override void OnUpdate(ref readonly SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderSystem.GetCurrentFramePayload();

        ref var gpuInstanceQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_gpuInstanceQueryID);

        foreach (var chunk in gpuInstanceQuery.GetChunkIterator())
        {
            var gpuInstanceRefs = chunk.GetComponentData<GPUInstanceRef>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var gpuInstance = gpuInstanceRefs.GetElementUnsafe(i);
                var entity = entities.GetElementUnsafe(i);

                payload.RemoveInstance(gpuInstance.gpuSceneIndex);
                systemAPI.World.EntityCommandBuffer.RemoveComponent<GPUInstanceRef>(entity);
            }
        }
    }
}