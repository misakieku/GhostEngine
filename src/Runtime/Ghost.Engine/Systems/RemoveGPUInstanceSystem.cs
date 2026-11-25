using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Engine.Systems;

[RenderPipelineSystem<GhostRenderPipelineSettings>]
internal class RemoveGPUInstanceSystem : SystemBase
{
    private readonly List<(uint gpuIndex, Entity entity, Identifier<MaterialPalette> palette)> _toRemove = new();
    private readonly Dictionary<uint, (Entity entity, uint originalSlot)> _liveBySlot = new();

    private RenderEngine _renderEngine = null!;

    private Identifier<EntityQuery> _gpuInstanceQueryID;
    private Identifier<EntityQuery> _liveGpuInstanceQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        _gpuInstanceQueryID = QueryBuilder.New()
            .WithAll<GPUInstanceRef>()
            .WithAbsent<MeshInstance>()
            .Build(systemAPI.World);

        _liveGpuInstanceQueryID = QueryBuilder.New()
            .WithAll<GPUInstanceRef, MeshInstance>()
            .Build(systemAPI.World);

        RequireQueryForUpdate(_gpuInstanceQueryID);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);

        _toRemove.Clear();
        ref var gpuInstanceQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_gpuInstanceQueryID);
        foreach (var chunk in gpuInstanceQuery.GetChunkIterator())
        {
            var gpuInstanceRefs = chunk.GetComponentData<GPUInstanceRef>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var gpuInstance = gpuInstanceRefs.GetElementUnsafe(i);
                var entity = entities.GetElementUnsafe(i);
                _toRemove.Add((gpuInstance.gpuInstanceIndex, entity, gpuInstance.materialPalette));
            }
        }

        if (_toRemove.Count == 0)
        {
            return;
        }

        // Pop highest GPU indices first so tail swaps never target another slot being removed in the same frame.
        _toRemove.Sort(static (a, b) => b.gpuIndex.CompareTo(a.gpuIndex));

        _liveBySlot.Clear();
        ref var liveQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_liveGpuInstanceQueryID);
        foreach (var chunk in liveQuery.GetChunkIterator())
        {
            var gpuInstanceRefs = chunk.GetComponentData<GPUInstanceRef>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var slot = gpuInstanceRefs.GetElementUnsafe(i).gpuInstanceIndex;
                _liveBySlot[slot] = (entities.GetElementUnsafe(i), slot);
            }
        }

        var em = systemAPI.World.EntityManager;
        for (var i = 0; i < _toRemove.Count; i++)
        {
            var item = _toRemove[i];
            _renderEngine.ResourceManager.ReleaseMaterialPalette(item.palette);
            systemAPI.EntityCommandBuffer.RemoveComponent<GPUInstanceRef>(item.entity);

            if (!payload.TryRemoveGpuInstanceSlot(item.gpuIndex, out var last))
            {
                continue;
            }

            if (last != item.gpuIndex && _liveBySlot.Remove(last, out var moved))
            {
                _liveBySlot[item.gpuIndex] = moved;
                em.GetComponent<GPUInstanceRef>(moved.entity).gpuInstanceIndex = item.gpuIndex;
            }
        }

        foreach (var kvp in _liveBySlot)
        {
            if (kvp.Key != kvp.Value.originalSlot)
            {
                payload.EnqueueRemoveSwap(kvp.Key, kvp.Value.originalSlot);
            }
        }

        _liveBySlot.Clear();
        _toRemove.Clear();
    }
}
