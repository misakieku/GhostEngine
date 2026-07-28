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
    private RenderEngine _renderSystem = null!;

    private Identifier<EntityQuery> _gpuInstanceQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderSystem = systemAPI.World.GetService<RenderEngine>();

        _gpuInstanceQueryID = QueryBuilder.New()
            .WithAll<GPUInstanceRef>()
            .WithAbsent<MeshInstance>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_gpuInstanceQueryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderSystem.GetCurrentFramePayload(systemAPI.Time.FrameIndex);
        payload.BeginRecord();

        ref var gpuInstanceQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_gpuInstanceQueryID);

        foreach (var chunk in gpuInstanceQuery.GetChunkIterator())
        {
            var gpuInstanceRefs = chunk.GetComponentData<GPUInstanceRef>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var gpuInstance = gpuInstanceRefs.GetElementUnsafe(i);
                var entity = entities.GetElementUnsafe(i);

                payload.RemoveInstance(gpuInstance.gpuInstanceIndex);
                _renderSystem.ResourceManager.ReleaseMaterialPalette(gpuInstance.materialPalette);

                systemAPI.World.EntityCommandBuffer.RemoveComponent<GPUInstanceRef>(entity);
            }
        }
    }
}
