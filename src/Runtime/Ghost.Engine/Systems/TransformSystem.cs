using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateBefore<CameraRenderSystem>]
[UpdateBefore<UpdateGPUInstanceSystem>]
[UpdateBefore<AddGPUInstanceSystem>]
[UpdateBefore<LightGatherSystem>]
public class TransformSystem : SystemBase
{
    private Identifier<EntityQuery> _standaloneQueryID;
    private Identifier<EntityQuery> _hierarchyQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _standaloneQueryID = QueryBuilder.New()
            .WithAll<LocalTransform, LocalToWorld>()
            .WithAbsent<Hierarchy>()
            .Build(systemAPI.World);

        _hierarchyQueryID = QueryBuilder.New()
            .WithAll<LocalTransform, LocalToWorld, Hierarchy>()
            .Build(systemAPI.World);
    }

    protected override bool ShouldUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        ref var q1 = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_standaloneQueryID);
        if (q1.HasMatchingEntity())
        {
            return true;
        }

        ref var q2 = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_hierarchyQueryID);
        return q2.HasMatchingEntity();
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        var em = systemAPI.World.EntityManager;

        ref var standaloneQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_standaloneQueryID);
        foreach (var chunk in standaloneQuery.GetChunkIterator())
        {
            if (LastSystemVersion != uint.MaxValue - 1 && !chunk.HasChanged<LocalTransform>(LastSystemVersion))
            {
                continue;
            }

            var locals = chunk.GetComponentData<LocalTransform>();
            var ltws = chunk.GetComponentDataRW<LocalToWorld>();
            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ltws[i].matrix = locals[i].ToMatrix();
            }
        }

        ref var hierarchyQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_hierarchyQueryID);
        foreach (var chunk in hierarchyQuery.GetChunkIterator())
        {
            var locals = chunk.GetComponentData<LocalTransform>();
            var ltws = chunk.GetComponentDataRW<LocalToWorld>();
            var hierarchies = chunk.GetComponentData<Hierarchy>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var hierarchy = ref hierarchies[i];
                if (hierarchy.parent != Entity.Invalid)
                {
                    continue;
                }

                ref var ltw = ref ltws[i];
                ltw.matrix = locals[i].ToMatrix();

                if (hierarchy.firstChild != Entity.Invalid)
                {
                    TransformUtility.UpdateChildTransforms(em, hierarchy.firstChild, in ltw.matrix, 0);
                }
            }
        }
    }
}
