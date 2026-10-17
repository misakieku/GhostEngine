using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Engine.Systems;

/// <summary>
/// Disposes unmanaged memory in <see cref="RenderVolumeDataContainer"/> when its parent <see cref="RenderVolume"/> has been destroyed.
/// </summary>
[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateBefore<RenderVolumeSystem>]
public class RenderVolumeCleanupSystem : SystemBase
{
    private Identifier<EntityQuery> _cleanupQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _cleanupQueryID = QueryBuilder.New()
            .WithAll<RenderVolumeDataContainer>()
            .WithAbsent<RenderVolume>()
            .Build(systemAPI.World);

        RequireQueryForUpdate(_cleanupQueryID);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cleanupQueryID);

        foreach (var chunk in query.GetChunkIterator())
        {
            var containers = chunk.GetComponentData<RenderVolumeDataContainer>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                containers[i].Dispose();
                systemAPI.EntityCommandBuffer.RemoveComponent<RenderVolumeDataContainer>(entities[i]);
            }
        }
    }

    protected override void OnCleanup(scoped in SystemAPI systemAPI)
    {
        using var scop = AllocationManager.CreateStackScope();
        using var cleanupQuery = QueryBuilder.New()
            .WithAll<RenderVolumeDataContainer>()
            .BuildWithoutCache(systemAPI.World, scop.AllocationHandle);

        foreach (var container in cleanupQuery.GetComponentIterator<RenderVolumeDataContainer>())
        {
            container.Dispose();
        }
    }
}
