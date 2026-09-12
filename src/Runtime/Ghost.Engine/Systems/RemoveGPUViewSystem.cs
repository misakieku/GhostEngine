using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
internal class RemoveGPUViewSystem : SystemBase
{
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _viewQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _viewQueryID = QueryBuilder.New()
            .WithAll<GPUViewRef>()
            .WithAbsent<Camera>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_viewQueryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);
        ref var viewQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_viewQueryID);

        foreach (var chunk in viewQuery.GetChunkIterator())
        {
            var views = chunk.GetComponentData<GPUViewRef>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var view = ref views[i];
                var entity = entities[i];

                payload.ReleaseView(view.viewId);
                systemAPI.World.EntityCommandBuffer.RemoveComponent<GPUViewRef>(entity);
            }
        }
    }
}
