using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Engine.Systems;

[UpdateAfter<RemoveGPUInstanceSystem>]
[RenderPipelineSystem<GhostRenderPipelineSettings>]
internal class AddGPUInstanceSystem : SystemBase
{
    private RenderSystem _renderSystem = null!;

    private Identifier<EntityQuery> _meshInstanceQueryID;

    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        _renderSystem = systemAPI.World.GetService<RenderSystem>();

        _meshInstanceQueryID = QueryBuilder.Create()
            .WithAll<MeshInstance, LocalToWorld>()
            .WithAbsent<GPUInstanceRef>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_meshInstanceQueryID);
    }

    protected override void OnUpdate(ref readonly SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderSystem.GetCurrentFramePayload();

        ref var meshInstanceQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_meshInstanceQueryID);

        foreach (var chunk in meshInstanceQuery.GetChunkIterator())
        {
            var meshInstances = chunk.GetComponentData<MeshInstance>();
            var localToWorlds = chunk.GetComponentData<LocalToWorld>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var meshInstance = ref meshInstances.GetElementUnsafe(i);
                var localToWorld = localToWorlds.GetElementUnsafe(i);
                var entity = entities.GetElementUnsafe(i);

                var index = payload.AddInstance(localToWorld.matrix, in meshInstance);
                systemAPI.World.EntityCommandBuffer.AddComponent(entity, new GPUInstanceRef { gpuSceneIndex = index });
            }
        }

        payload.EndRecord();
    }
}
