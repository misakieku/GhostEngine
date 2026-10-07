using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Entities;
using Ghost.Graphics;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Systems;

/// <summary>
/// Gathers active directional and punctual lights from ECS and populates the per-frame render payload.
/// </summary>
[RenderPipelineSystem<GhostRenderPipelineSettings>]
[UpdateAfter<CameraRenderSystem>]
internal class LightGatherSystem : SystemBase
{
    private RenderEngine _renderEngine = null!;
    private Identifier<EntityQuery> _directionalLightQueryID;
    private Identifier<EntityQuery> _punctualLightQueryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();

        var builder = QueryBuilder.New();

        _directionalLightQueryID = builder
            .WithAll<DirectionalLight, LocalToWorld>()
            .Build(systemAPI.World, false);

        builder.Reset();

        _punctualLightQueryID = builder
            .WithAll<PunctualLight, LocalToWorld>()
            .Build(systemAPI.World);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);
        GatherDirectionalLight(systemAPI, payload);
        GatherPunctualLights(systemAPI, payload);
    }

    private void GatherDirectionalLight(scoped in SystemAPI systemAPI, GhostRenderPayload payload)
    {
        ref var dirQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_directionalLightQueryID);
        if (!dirQuery.HasMatchingEntity())
        {
            return;
        }

        var bestScore = -1.0f;
        var primaryIndex = -1;

        foreach (var chunk in dirQuery.GetChunkIterator())
        {
            var lights = chunk.GetComponentData<DirectionalLight>();
            var transforms = chunk.GetComponentData<LocalToWorld>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var light = ref lights[i];
                ref readonly var transform = ref transforms[i];

                if (light.intensity <= 0.0f)
                {
                    continue;
                }

                var forward = transform.matrix.c2.xyz;
                if (math.lengthsq(forward) > 1e-6f)
                {
                    forward = math.normalize(forward);
                }
                else
                {
                    forward = new float3(0.0f, -1.0f, 0.0f); // Default downward
                }

                var gpuLight = new GPUDirectionalLight
                {
                    directionWS = forward,
                    castShadows = light.castShadows ? 1u : 0u,
                    color = light.color * light.intensity,
                    shadowBiasMultiplier = light.shadowBiasMultiplier > 0.0f ? light.shadowBiasMultiplier : 1.0f,
                    cascadeSplits = default,
                    shadowMatrix0 = float4x4.identity,
                    shadowMatrix1 = float4x4.identity,
                    shadowMatrix2 = float4x4.identity,
                    shadowMatrix3 = float4x4.identity
                };

                var addedIndex = (int)payload.AddDirectionalLight(in gpuLight);

                // Luminance calculation: standard Rec. 709 coefficients
                var luminance = (light.color.x * 0.2126f + light.color.y * 0.7152f + light.color.z * 0.0722f) * light.intensity;
                // Shadow casters get priority; among casters, pick highest luminance
                var score = luminance + (light.castShadows ? 10000.0f : 0.0f);

                if (light.castShadows && score > bestScore)
                {
                    bestScore = score;
                    primaryIndex = addedIndex;
                }
            }
        }

        payload.SetPrimaryDirectionalLightIndex(primaryIndex);
    }

    private void GatherPunctualLights(scoped in SystemAPI systemAPI, GhostRenderPayload payload)
    {
        ref var punctualQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_punctualLightQueryID);
        if (!punctualQuery.HasMatchingEntity())
        {
            return;
        }

        foreach (var chunk in punctualQuery.GetChunkIterator())
        {
            var lights = chunk.GetComponentData<PunctualLight>();
            var transforms = chunk.GetComponentData<LocalToWorld>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var light = ref lights[i];
                ref readonly var transform = ref transforms[i];

                if (light.range <= 0.0001f || light.intensity <= 0.0f)
                {
                    continue;
                }

                var posWS = transform.matrix.c3.xyz;
                var forward = transform.matrix.c2.xyz;
                var dirWS = math.lengthsq(forward) > 1e-6f ? math.normalize(forward) : new float3(0.0f, 0.0f, 1.0f);
                var invRangeSq = 1.0f / (light.range * light.range);

                var spotAngleScale = 0.0f;
                var spotAngleOffset = 0.0f;
                if (light.type == PunctualLightType.Spot)
                {
                    var inner = math.min(light.innerSpotAngle, light.outerSpotAngle);
                    var outer = math.max(light.innerSpotAngle, light.outerSpotAngle);
                    var cosInner = math.cos(inner);
                    var cosOuter = math.cos(outer);
                    spotAngleScale = 1.0f / math.max(0.001f, cosInner - cosOuter);
                    spotAngleOffset = -cosOuter * spotAngleScale;
                }

                var flags = ((uint)light.type & 0xFu) | ((light.shadowSize & 0xFFFFu) << 4);

                var gpuLight = new GPUPunctualLight
                {
                    positionWS = posWS,
                    range = light.range,
                    color = light.color * light.intensity,
                    lightTypeAndFlags = flags,
                    directionWS = dirWS,
                    spotAngleScale = spotAngleScale,
                    spotAngleOffset = spotAngleOffset,
                    invRangeSq = invRangeSq,
                    shadowIndex = -1,
                    sourceRadius = light.sourceRadius
                };

                payload.AddPunctualLight(in gpuLight);
            }
        }
    }
}
