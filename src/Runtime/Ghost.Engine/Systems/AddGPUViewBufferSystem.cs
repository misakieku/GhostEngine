using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateBefore<CameraRenderSystem>]
internal class AddGPUViewBufferSystem : SystemBase
{
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _cameraQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _cameraQueryID = QueryBuilder.New()
            .WithAll<Camera, LocalToWorld>()
            .WithAbsent<GPUViewBufferContainer>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_cameraQueryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);
        var db = _renderEngine.GraphicsEngine.ResourceDatabase;

        foreach (var chunk in cameraQuery.GetChunkIterator())
        {
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var entity = entities[i];

                var container = new GPUViewBufferContainer
                {
                    depthTarget = db.CreateEmpty().AsTexture(),
                    hzbMipCount = 0,
                    historyWidth = 0,
                    historyHeight = 0,
                    isHistoryValid = false
                };

                for (var m = 0; m < 16; m++)
                {
                    container.hzbHistory[m] = db.CreateEmpty().AsTexture();
                }

                // This should be fine, as we are running in a single-threaded context and the entity is not being modified by other systems at this point.
                systemAPI.World.EntityManager.AddComponent(entity, container);
            }
        }
    }
}
