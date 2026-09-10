using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
internal class RemoveGPUViewBufferSystem : SystemBase
{
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _containerQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _containerQueryID = QueryBuilder.New()
            .WithAll<GPUViewBufferContainer>()
            .WithAbsent<Camera>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_containerQueryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        ref var containerQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_containerQueryID);
        var db = _renderEngine.GraphicsEngine.ResourceDatabase;

        foreach (var chunk in containerQuery.GetChunkIterator())
        {
            var containers = chunk.GetComponentData<GPUViewBufferContainer>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var container = ref containers[i];
                var entity = entities[i];

                if (container.depthTarget.IsValid)
                {
                    db.ReleaseResource(container.depthTarget.AsResource());
                }

                for (var m = 0; m < 16; m++)
                {
                    if (container.hzbHistory[m].IsValid)
                    {
                        db.ReleaseResource(container.hzbHistory[m].AsResource());
                    }
                }

                systemAPI.World.EntityCommandBuffer.RemoveComponent<GPUViewBufferContainer>(entity);
            }
        }
    }
}
