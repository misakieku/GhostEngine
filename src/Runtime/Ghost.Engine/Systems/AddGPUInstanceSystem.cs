using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateAfter<UpdateGPUInstanceSystem>]
internal class AddGPUInstanceSystem : SystemBase
{
    private struct AddGPUInstanceJob : IJobChunk
    {
        public World world;
        public ResourceManager resourceManager;
        public IResourceDatabase resourceDatabase;
        public GhostRenderPayload payload;

        public void Execute(ChunkView chunk, ref readonly JobExecutionContext ctx)
        {
            var meshInstances = chunk.GetComponentData<MeshInstance>();
            var localToWorlds = chunk.GetComponentData<LocalToWorld>();
            var entities = chunk.GetEntities();

            ref var cmd = ref world.GetThreadLocalEntityCommandBufferRef(ctx.ThreadIndex);

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var meshInstance = ref meshInstances.GetElementUnsafe(i);
                var localToWorld = localToWorlds.GetElementUnsafe(i);
                var entity = entities.GetElementUnsafe(i);

                if (meshInstance.mesh.IsInvalid)
                {
                    continue;
                }

                var meshResult = resourceManager.GetMeshReference(meshInstance.mesh);
                if (meshResult.IsFailure)
                {
                    continue;
                }

                ref readonly var mesh = ref meshResult.Value;
                if (mesh.MeshletCount <= 0 || mesh.MeshDataBuffer.IsInvalid)
                {
                    continue;
                }

                var bindlessIndex = resourceDatabase.GetBindlessIndex(mesh.MeshDataBuffer.AsResource());
                if (bindlessIndex == uint.MaxValue)
                {
                    continue;
                }

                var index = payload.AddInstance(localToWorld.matrix, in meshInstance);
                var materialPalette = meshInstance.materialPalette;

                cmd.AddComponent(entity, new GPUInstanceRef { gpuInstanceIndex = index, materialPalette = materialPalette });
            }
        }
    }

    private RenderEngine _renderEngine = null!;

    private Identifier<EntityQuery> _meshInstanceQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _meshInstanceQueryID = QueryBuilder.New()
            .WithAll<MeshInstance, LocalToWorld>()
            .WithAbsent<GPUInstanceRef>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_meshInstanceQueryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);

        ref var meshInstanceQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_meshInstanceQueryID);
        var job = new AddGPUInstanceJob
        {
            world = systemAPI.World,
            resourceManager = _renderEngine.ResourceManager,
            resourceDatabase = _renderEngine.GraphicsEngine.ResourceDatabase,
            payload = payload
        };

        var handle = meshInstanceQuery.ScheduleChunkParallel(job, 1, default);
        systemAPI.World.JobScheduler.Wait(handle);
    }
}
