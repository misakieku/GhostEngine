using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Systems;

/// <summary>
/// Evaluates spatial bounds, calculates effective weights, and blends active volume components into the render payload for each camera.
/// </summary>
[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateBefore<CameraRenderSystem>]
[UpdateAfter<AddGPUViewSystem>]
public unsafe class RenderVolumeSystem : SystemBase
{
    private struct ActiveVolume : IComparable<ActiveVolume>
    {
        public Entity entity;
        public float weight;
        public float priority;

        public readonly int CompareTo(ActiveVolume other)
        {
            return priority.CompareTo(other.priority);
        }
    }

    public GhostRenderPayload? OverridePayload { get; set; }

    private RenderEngine? _renderEngine;
    private Identifier<EntityQuery> _cameraQueryID;
    private Identifier<EntityQuery> _volumeQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        systemAPI.World.TryGetService<RenderEngine>(out _renderEngine);

        _cameraQueryID = QueryBuilder.New()
            .WithAll<Camera, LocalToWorld, GPUViewRef>()
            .Build(systemAPI.World);

        _volumeQueryID = QueryBuilder.New()
            .WithAll<RenderVolume, RenderVolumeDataContainer>()
            .Build(systemAPI.World);

        RequireQueryForUpdate(_cameraQueryID);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        ref var volumeQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_volumeQueryID);
        if (!volumeQuery.HasMatchingEntity())
        {
            return;
        }

        var payload = OverridePayload ?? (_renderEngine != null ? (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex) : null);
        if (payload == null)
        {
            return;
        }
        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);

        using var scope = AllocationManager.CreateStackScope();
        Span<byte> blendBuffer = stackalloc byte[512];
        fixed (byte* pBlendBase = blendBuffer)
        {
            foreach (var cameraChunk in cameraQuery.GetChunkIterator())
            {
                var cameras = cameraChunk.GetComponentData<Camera>();
                var cameraLtws = cameraChunk.GetComponentData<LocalToWorld>();
                var viewRefs = cameraChunk.GetComponentData<GPUViewRef>();

                for (var c = 0; c < cameraChunk.EntityCount; c++)
                {
                    ref readonly var camera = ref cameras[c];
                    ref readonly var cameraLtw = ref cameraLtws[c];
                    ref readonly var viewRef = ref viewRefs[c];

                    var camPos = cameraLtw.matrix.c3.xyz;
                    var camLayer = camera.renderingLayerMask;
                    var viewId = viewRef.viewId;

                    using var activeVolumes = new UnsafeList<ActiveVolume>(16, scope.AllocationHandle);

                    // 1. Spatial and layer culling against all volumes
                    foreach (var volumeChunk in volumeQuery.GetChunkIterator())
                    {
                        var volumes = volumeChunk.GetComponentData<RenderVolume>();
                        var entities = volumeChunk.GetEntities();

                        for (var v = 0; v < volumeChunk.EntityCount; v++)
                        {
                            ref readonly var vol = ref volumes[v];
                            if ((vol.renderingLayerMask & camLayer) == 0 || vol.weight <= 0.0f)
                            {
                                continue;
                            }

                            var effectiveWeight = 0.0f;
                            if (vol.isGlobal)
                            {
                                effectiveWeight = math.clamp(vol.weight, 0.0f, 1.0f);
                            }
                            else
                            {
                                var volPos = float3.zero;
                                var volScale = new float3(1.0f, 1.0f, 1.0f);

                                if (systemAPI.World.EntityManager.HasComponent<LocalToWorld>(entities[v]))
                                {
                                    ref readonly var volLtw = ref systemAPI.World.EntityManager.GetComponent<LocalToWorld>(entities[v]);
                                    volPos = volLtw.matrix.c3.xyz;
                                    volScale = new float3(
                                        math.length(volLtw.matrix.c0.xyz),
                                        math.length(volLtw.matrix.c1.xyz),
                                        math.length(volLtw.matrix.c2.xyz)
                                    );
                                }

                                var distToBoundary = 0.0f;
                                if (vol.shape == VolumeShape.Sphere)
                                {
                                    var radius = math.max(volScale.x, math.max(volScale.y, volScale.z)) * 0.5f;
                                    var d = math.distance(camPos, volPos);
                                    distToBoundary = d - radius;
                                }
                                else
                                {
                                    var halfExtents = volScale * 0.5f;
                                    var delta = math.abs(camPos - volPos) - halfExtents;
                                    var outside = math.max(delta, float3.zero);
                                    distToBoundary = math.length(outside);
                                }

                                if (distToBoundary <= 0.0f)
                                {
                                    effectiveWeight = vol.weight;
                                }
                                else if (vol.blendDistance > 0.0001f && distToBoundary < vol.blendDistance)
                                {
                                    var t = 1.0f - (distToBoundary / vol.blendDistance);
                                    t = t * t * (3.0f - 2.0f * t);
                                    effectiveWeight = vol.weight * t;
                                }
                            }

                            if (effectiveWeight > 0.0001f)
                            {
                                activeVolumes.Add(new ActiveVolume
                                {
                                    entity = entities[v],
                                    weight = math.clamp(effectiveWeight, 0.0f, 1.0f),
                                    priority = vol.priority
                                });
                            }
                        }
                    }

                    if (activeVolumes.Count == 0)
                    {
                        continue;
                    }

                    // 2. Sort active volumes ascending by priority
                    activeVolumes.AsSpan().Sort();

                    // 3. Gather all unique component type IDs present across active containers
                    using var activeTypes = new UnsafeList<ushort>(16, scope.AllocationHandle);
                    for (var a = 0; a < activeVolumes.Count; a++)
                    {
                        ref readonly var container = ref systemAPI.World.EntityManager.GetComponent<RenderVolumeDataContainer>(activeVolumes[a].entity);
                        var entries = container.Entries;
                        for (var e = 0; e < entries.Length; e++)
                        {
                            var tid = entries[e].typeId;
                            var exists = false;
                            for (var k = 0; k < activeTypes.Count; k++)
                            {
                                if (activeTypes[k] == tid)
                                {
                                    exists = true;
                                    break;
                                }
                            }

                            if (!exists)
                            {
                                activeTypes.Add(tid);
                            }
                        }
                    }

                    // 4. Blend each component type across active volumes
                    for (var t = 0; t < activeTypes.Count; t++)
                    {
                        var typeId = activeTypes[t];
                        if (VolumeRegistry.TryGetBlender(typeId, out var blender))
                        {
                            var pTarget = blender.size <= 512
                                ? pBlendBase
                                : (byte*)MemoryUtility.Malloc((nuint)blender.size);

                            blender.pResetFunc(pTarget);

                            for (var a = 0; a < activeVolumes.Count; a++)
                            {
                                ref readonly var container = ref systemAPI.World.EntityManager.GetComponent<RenderVolumeDataContainer>(activeVolumes[a].entity);
                                if (container.TryGetPtr(typeId, out var pSource))
                                {
                                    blender.pBlendFunc(pTarget, pSource, activeVolumes[a].weight);
                                }
                            }

                            payload.SetViewVolumeData(viewId, typeId, pTarget, blender.size);

                            if (blender.size > 512)
                            {
                                MemoryUtility.Free(pTarget);
                            }
                        }
                    }
                }
            }
        }
    }
}
