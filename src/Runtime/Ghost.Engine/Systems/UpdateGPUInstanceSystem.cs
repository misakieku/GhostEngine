using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateAfter<RemoveGPUInstanceSystem>]
[UpdateBefore<AddGPUInstanceSystem>]
internal class UpdateGPUInstanceSystem : SystemBase
{
    private RenderSystem _renderSystem = null!;
    private Identifier<EntityQuery> _gpuInstanceQueryID;

    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        _renderSystem = systemAPI.World.GetService<RenderSystem>();

        _gpuInstanceQueryID = QueryBuilder.New()
            .WithAll<LocalToWorld, MeshInstance, GPUInstanceRef>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_gpuInstanceQueryID);
    }

    protected override void OnUpdate(ref readonly SystemAPI systemAPI)
    {
        var playload = (GhostRenderPayload)_renderSystem.GetCurrentFramePayload();

        ref var instanceQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_gpuInstanceQueryID);

        foreach (var chunk in instanceQuery.GetChunkIterator())
        {
            if (!chunk.HasChanged<MeshInstance>(LastSystemVersion))
            {
                continue;
            }

            var ltws = chunk.GetComponentData<LocalToWorld>();
            var meshs = chunk.GetComponentData<MeshInstance>();
            var gpuInstances = chunk.GetComponentData<GPUInstanceRef>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var ltw = ref ltws.GetElementUnsafe(i);
                ref readonly var mesh = ref meshs.GetElementUnsafe(i);
                ref readonly var instance = ref gpuInstances.GetElementUnsafe(i);

                playload.UpdateInstance(instance.gpuInstanceIndex, ltw.matrix, in mesh);
            }
        }
    }
}
