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

    internal void InitializeQueries(World world)
    {
        _directionalLightQueryID = QueryBuilder.New()
            .WithAll<DirectionalLight, LocalToWorld>()
            .Build(world, true);

        _punctualLightQueryID = QueryBuilder.New()
            .WithAll<PunctualLight, LocalToWorld>()
            .Build(world, true);
    }

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _renderEngine = systemAPI.World.GetService<RenderEngine>();
        InitializeQueries(systemAPI.World);
    }

    internal void Gather(scoped in SystemAPI systemAPI, GhostRenderPayload payload)
    {
        GatherDirectionalLight(systemAPI, payload);
        GatherPunctualLights(systemAPI, payload);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var payload = (GhostRenderPayload)_renderEngine.GetCurrentFramePayload(systemAPI.Time.FrameIndex);
        Gather(systemAPI, payload);
    }

    private void GatherDirectionalLight(scoped in SystemAPI systemAPI, GhostRenderPayload payload)
    {
        ref var dirQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_directionalLightQueryID);
        if (dirQuery.CalculateEntityCount() == 0)
        {
            return;
        }

        DirectionalLight bestLight = default;
        LocalToWorld bestTransform = default;
        var foundSun = false;
        var bestScore = -1.0f;

        foreach (var chunk in dirQuery.GetChunkIterator())
        {
            var lights = chunk.GetComponentData<DirectionalLight>();
            var transforms = chunk.GetComponentData<LocalToWorld>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var light = ref lights[i];
                ref readonly var transform = ref transforms[i];

                // Luminance calculation: standard Rec. 709 coefficients
                var luminance = (light.color.x * 0.2126f + light.color.y * 0.7152f + light.color.z * 0.0722f) * light.intensity;
                // Shadow casters get priority; among casters, pick highest luminance
                var score = luminance + (light.castShadows ? 10000.0f : 0.0f);

                if (!foundSun || score > bestScore)
                {
                    foundSun = true;
                    bestScore = score;
                    bestLight = light;
                    bestTransform = transform;
                }
            }
        }

        if (foundSun)
        {
            var forward = bestTransform.matrix.c2.xyz;
            if (math.lengthsq(forward) > 1e-6f)
            {
                forward = math.normalize(forward);
            }
            else
            {
                forward = new float3(0.0f, -1.0f, 0.0f); // Default downward
            }

            var gpuSun = new GPUDirectionalLight
            {
                directionWS = forward,
                castShadows = bestLight.castShadows ? 1u : 0u,
                color = bestLight.color * bestLight.intensity,
                shadowBiasMultiplier = bestLight.shadowBiasMultiplier > 0.0f ? bestLight.shadowBiasMultiplier : 1.0f,
                cascadeSplits = default,
                shadowMatrix0 = float4x4.identity,
                shadowMatrix1 = float4x4.identity,
                shadowMatrix2 = float4x4.identity,
                shadowMatrix3 = float4x4.identity
            };

            payload.SetDirectionalLight(in gpuSun);
        }
    }

    private void GatherPunctualLights(scoped in SystemAPI systemAPI, GhostRenderPayload payload)
    {
        ref var punctualQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_punctualLightQueryID);
        if (punctualQuery.CalculateEntityCount() == 0)
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

                float spotAngleScale = 0.0f;
                float spotAngleOffset = 0.0f;
                if (light.type == PunctualLightType.Spot)
                {
                    var inner = math.min(light.innerSpotAngle, light.outerSpotAngle);
                    var outer = math.max(light.innerSpotAngle, light.outerSpotAngle);
                    var cosInner = math.cos(inner);
                    var cosOuter = math.cos(outer);
                    spotAngleScale = 1.0f / math.max(0.001f, cosInner - cosOuter);
                    spotAngleOffset = -cosOuter * spotAngleScale;
                }

                var gpuLight = new GPUPunctualLight
                {
                    positionWS = posWS,
                    range = light.range,
                    color = light.color * light.intensity,
                    lightTypeAndFlags = (uint)light.type & 0xFu,
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
