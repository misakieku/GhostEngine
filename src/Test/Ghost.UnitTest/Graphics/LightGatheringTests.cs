using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

[TestClass]
[DoNotParallelize]
public class LightGatheringTests
{
    private JobScheduler _scheduler = null!;
    private World _world = null!;

    [TestInitialize]
    public void Setup()
    {
        var desc = new JobSchedulerDesc
        {
            DependencyChainCapacity = 0,
            ThreadCount = 0,
        };
        _scheduler = new JobScheduler(in desc);
        _world = World.Create(_scheduler, 64);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _world.Dispose();
        _scheduler.Dispose();
    }

    [TestMethod]
    public void TestGhostRenderPayload_PunctualLightsManagement()
    {
        using var payload = new GhostRenderPayload(null!);

        Assert.AreEqual(0u, payload.PunctualLightCount);
        Assert.AreEqual(0, payload.PunctualLights.Length);
        Assert.IsFalse(payload.HasDirectionalLight);

        var light = new GPUPunctualLight
        {
            positionWS = new float3(1.0f, 2.0f, 3.0f),
            range = 10.0f,
            color = new float3(1.0f, 0.5f, 0.2f),
            lightTypeAndFlags = 0,
            directionWS = new float3(0.0f, 0.0f, 1.0f),
            spotAngleScale = 1.0f,
            spotAngleOffset = 0.0f,
            invRangeSq = 0.01f,
            shadowIndex = -1,
            sourceRadius = 0.05f
        };

        payload.AddPunctualLight(in light);

        Assert.AreEqual(1u, payload.PunctualLightCount);
        Assert.AreEqual(1, payload.PunctualLights.Length);
        Assert.AreEqual(new float3(1.0f, 2.0f, 3.0f), payload.PunctualLights[0].positionWS);
        Assert.AreEqual(10.0f, payload.PunctualLights[0].range);

        var sun = new GPUDirectionalLight
        {
            directionWS = new float3(0.0f, -1.0f, 0.0f),
            castShadows = 1,
            color = new float3(2.0f, 2.0f, 2.0f),
            shadowBiasMultiplier = 1.0f
        };

        payload.SetDirectionalLight(in sun);
        Assert.IsTrue(payload.HasDirectionalLight);
        Assert.AreEqual(new float3(0.0f, -1.0f, 0.0f), payload.CurrentSunLight.directionWS);
        Assert.AreEqual(1u, payload.CurrentSunLight.castShadows);

        payload.Reset();

        Assert.AreEqual(0u, payload.PunctualLightCount);
        Assert.AreEqual(0, payload.PunctualLights.Length);
        Assert.IsFalse(payload.HasDirectionalLight);
    }

    [TestMethod]
    public void TestLightGatherSystem_GathersAndPacksLights()
    {
        var system = new LightGatherSystem();
        system.InitializeQueries(_world);

        // 1. Create primary sun (Directional light)
        var sunEntity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(sunEntity, new DirectionalLight
        {
            color = new float3(1.0f, 0.95f, 0.9f),
            intensity = 2.5f,
            castShadows = true,
            shadowBiasMultiplier = 1.2f,
            sunDiskAngularDiameter = 0.00925f,
            volumetricScattering = 1.0f,
            affectAtmosphere = true
        });
        _world.EntityManager.AddComponent(sunEntity, new LocalToWorld
        {
            matrix = float4x4.identity
        });

        // 2. Create Point Light
        var pointEntity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(pointEntity, PunctualLight.CreatePoint(new float3(1.0f, 0.0f, 0.0f), 5.0f, 15.0f));
        _world.EntityManager.AddComponent(pointEntity, new LocalToWorld
        {
            matrix = new float4x4(
                1, 0, 0, 10,
                0, 1, 0, 20,
                0, 0, 1, 30,
                0, 0, 0, 1)
        });

        // 3. Create Spot Light
        var spotEntity = _world.EntityManager.CreateEntity();
        var innerRad = math.radians(20.0f);
        var outerRad = math.radians(45.0f);
        _world.EntityManager.AddComponent(spotEntity, PunctualLight.CreateSpot(new float3(0.0f, 1.0f, 0.0f), 3.0f, 25.0f, innerRad, outerRad));
        _world.EntityManager.AddComponent(spotEntity, new LocalToWorld
        {
            matrix = new float4x4(
                1, 0, 0, 5,
                0, 1, 0, 5,
                0, 0, 1, 5,
                0, 0, 0, 1)
        });

        // Gather into payload
        using var payload = new GhostRenderPayload(null!);
        var systemAPI = new SystemAPI { World = _world, Time = new TimeData() };

        system.Gather(in systemAPI, payload);

        // Verify Directional Light
        Assert.IsTrue(payload.HasDirectionalLight);
        Assert.AreEqual(1u, payload.CurrentSunLight.castShadows);
        Assert.AreEqual(new float3(2.5f, 2.375f, 2.25f), payload.CurrentSunLight.color);
        Assert.AreEqual(1.2f, payload.CurrentSunLight.shadowBiasMultiplier);

        // Verify Punctual Lights (2 lights gathered)
        Assert.AreEqual(2u, payload.PunctualLightCount);

        // Find Point and Spot
        var foundPoint = false;
        var foundSpot = false;

        for (var i = 0; i < payload.PunctualLights.Length; i++)
        {
            ref readonly var pl = ref payload.PunctualLights[i];
            if (pl.lightTypeAndFlags == (uint)PunctualLightType.Point)
            {
                foundPoint = true;
                Assert.AreEqual(new float3(10.0f, 20.0f, 30.0f), pl.positionWS);
                Assert.AreEqual(15.0f, pl.range);
                Assert.AreEqual(new float3(5.0f, 0.0f, 0.0f), pl.color);
                Assert.AreEqual(1.0f / (15.0f * 15.0f), pl.invRangeSq, 1e-5f);
            }
            else if (pl.lightTypeAndFlags == (uint)PunctualLightType.Spot)
            {
                foundSpot = true;
                Assert.AreEqual(new float3(5.0f, 5.0f, 5.0f), pl.positionWS);
                Assert.AreEqual(25.0f, pl.range);
                Assert.AreEqual(new float3(0.0f, 3.0f, 0.0f), pl.color);
                Assert.IsGreaterThan(0.0f, pl.spotAngleScale);
            }
        }

        Assert.IsTrue(foundPoint);
        Assert.IsTrue(foundSpot);
    }

    [TestMethod]
    public void TestLightGatherSystem_DirectionalLightCasterPriority()
    {
        var system = new LightGatherSystem();
        system.InitializeQueries(_world);

        // Weak shadow caster
        var caster = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(caster, new DirectionalLight
        {
            color = new float3(1.0f, 1.0f, 1.0f),
            intensity = 0.5f,
            castShadows = true,
            shadowBiasMultiplier = 2.0f
        });
        _world.EntityManager.AddComponent(caster, new LocalToWorld { matrix = float4x4.identity });

        // Very bright non-caster
        var nonCaster = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(nonCaster, new DirectionalLight
        {
            color = new float3(1.0f, 1.0f, 1.0f),
            intensity = 100.0f,
            castShadows = false,
            shadowBiasMultiplier = 1.0f
        });
        _world.EntityManager.AddComponent(nonCaster, new LocalToWorld { matrix = float4x4.identity });

        using var payload = new GhostRenderPayload(null!);
        var systemAPI = new SystemAPI { World = _world, Time = new TimeData() };

        system.Gather(in systemAPI, payload);

        // Shadow caster must take priority
        Assert.IsTrue(payload.HasDirectionalLight);
        Assert.AreEqual(1u, payload.CurrentSunLight.castShadows);
        Assert.AreEqual(new float3(0.5f, 0.5f, 0.5f), payload.CurrentSunLight.color);
        Assert.AreEqual(2.0f, payload.CurrentSunLight.shadowBiasMultiplier);
    }
}
