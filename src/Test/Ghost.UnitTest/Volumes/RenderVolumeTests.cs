using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Systems;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Volumes;

[TestClass]
[DoNotParallelize]
public class RenderVolumeTests
{
    private JobScheduler _scheduler = null!;
    private World _world = null!;

    private class TestCleanupGroup : SystemGroup
    {
        public TestCleanupGroup()
        {
            AddSystem<RenderVolumeCleanupSystem>();
            SortSystems();
        }
    }

    private class TestVolumeGroup : SystemGroup
    {
        public RenderVolumeSystem VolumeSystem { get; } = new();

        public TestVolumeGroup()
        {
            AddSystem(VolumeSystem);
            SortSystems();
        }
    }

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
    public void TestEnvironmentSettings_Behavior()
    {
        var def = EnvironmentSettings.Default;
        Assert.IsTrue(def.skyType.HasValue);
        Assert.AreEqual(SkyType.HDRI, def.skyType.Value);
        Assert.AreEqual(SkyType.HDRI, def.skyType.GetValueOrDefault(SkyType.HDRI));

        var target = EnvironmentSettings.Default;
        var source = new EnvironmentSettings { skyType = SkyType.HDRI };
        EnvironmentSettings.Blend(ref target, in source, 1.0f);
        Assert.AreEqual(SkyType.HDRI, target.skyType.GetValueOrDefault(SkyType.HDRI));

        unsafe
        {
            var s = sizeof(EnvironmentSettings);
            var sourceBytes = (byte*)&source;
            Assert.AreEqual(2, s);
            Assert.AreEqual(1, sourceBytes[0], "sourceBytes[0]");
            Assert.AreEqual(1, sourceBytes[1], "sourceBytes[1]");
        }
        var container = RenderVolumeDataContainer.Create();
        container.Set(new EnvironmentSettings { skyType = SkyType.HDRI });
        Assert.IsTrue(container.TryGet<EnvironmentSettings>(out var retrieved));
        Assert.AreEqual(SkyType.HDRI, retrieved.skyType.GetValueOrDefault(SkyType.HDRI));
        container.Dispose();

        var payload = new GhostRenderPayload(null!);
        try
        {
            ref readonly var fromPayloadDefault = ref payload.GetViewVolumeData<EnvironmentSettings>(1);
            Assert.AreEqual(SkyType.HDRI, fromPayloadDefault.skyType.GetValueOrDefault(SkyType.HDRI));

            payload.SetViewVolumeData(1, in source);
            ref readonly var fromPayloadSet = ref payload.GetViewVolumeData<EnvironmentSettings>(1);
            Assert.AreEqual(SkyType.HDRI, fromPayloadSet.skyType.GetValueOrDefault(SkyType.HDRI));
        }
        finally
        {
            payload.Dispose();
        }
    }

    [TestMethod]
    public void TestVolumeComponent_GeneratedBlend_InterpolatesAndHardcuts()
    {
        var target = HDRISettings.Default;
        var fakeHandle = new Handle<GPUTexture>(42, 1);

        var source = new HDRISettings
        {
            texture = fakeHandle,
            intensity = 3.0f,
            // rotation and tint left null (not overridden)
        };

        // Blend at weight 0.5
        HDRISettings.Blend(ref target, in source, 0.5f);

        // Intensity: lerp(1.0, 3.0, 0.5) = 2.0
        Assert.AreEqual(2.0f, target.intensity!.Value, 1e-4f);
        // Texture: hardcut
        Assert.AreEqual(fakeHandle, target.texture!.Value);
        // Rotation: untouched default (0.0)
        Assert.AreEqual(0.0f, target.rotation!.Value, 1e-4f);
        // Tint: untouched default (1, 1, 1)
        Assert.AreEqual(new float3(1.0f, 1.0f, 1.0f), target.tint!.Value);
    }

    [TestMethod]
    public void TestVolumeComponent_WeightZeroAndOne_SkipsProperly()
    {
        var target = HDRISettings.Default;
        var source = new HDRISettings
        {
            intensity = 5.0f
        };

        // Weight <= 0.0 skips blend completely
        HDRISettings.Blend(ref target, in source, 0.0f);
        Assert.AreEqual(1.0f, target.intensity!.Value);

        // Weight >= 1.0 assigns directly without lerp
        HDRISettings.Blend(ref target, in source, 1.0f);
        Assert.AreEqual(5.0f, target.intensity!.Value);
    }

    [TestMethod]
    public void TestVolumeComponent_HardcutAttribute_ForcesHardcutOnNumeric()
    {
        var target = BloomSettings.Default;
        var source = new BloomSettings
        {
            threshold = 0.5f,
            intensity = 2.0f,
            clamp = 100.0f, // [Hardcut]
            highQuality = true // bool -> hardcut
        };

        BloomSettings.Blend(ref target, in source, 0.5f);

        // threshold is lerp: lerp(0.9, 0.5, 0.5) = 0.7
        Assert.AreEqual(0.7f, target.threshold!.Value, 1e-4f);
        // intensity is lerp: lerp(1.0, 2.0, 0.5) = 1.5
        Assert.AreEqual(1.5f, target.intensity!.Value, 1e-4f);
        // clamp has [Hardcut], so it should be directly 100.0, not lerped!
        Assert.AreEqual(100.0f, target.clamp!.Value, 1e-4f);
        // highQuality is bool, so hardcut
        Assert.IsTrue(target.highQuality!.Value);
    }

    [TestMethod]
    public void TestRenderVolumeDataContainer_SetGetHasRemove()
    {
        using var container = RenderVolumeDataContainer.Create();

        Assert.AreEqual(0, container.ComponentCount);
        Assert.IsFalse(container.Has<HDRISettings>());

        var sky = new HDRISettings
        {
            intensity = 2.5f,
            rotation = 1.57f
        };

        container.Set(sky);
        Assert.AreEqual(1, container.ComponentCount);
        Assert.IsTrue(container.Has<HDRISettings>());

        Assert.IsTrue(container.TryGet<HDRISettings>(out var retrievedSky));
        Assert.AreEqual(2.5f, retrievedSky.intensity!.Value, 1e-4f);
        Assert.AreEqual(1.57f, retrievedSky.rotation!.Value, 1e-4f);

        // Updating in-place should not increase component count
        sky.intensity = 4.0f;
        container.Set(sky);
        Assert.AreEqual(1, container.ComponentCount);
        Assert.IsTrue(container.TryGet<HDRISettings>(out retrievedSky));
        Assert.AreEqual(4.0f, retrievedSky.intensity!.Value, 1e-4f);

        // Add second component type
        var bloom = new BloomSettings
        {
            intensity = 3.0f
        };
        container.Set(bloom);
        Assert.AreEqual(2, container.ComponentCount);
        Assert.IsTrue(container.Has<BloomSettings>());

        // Remove component
        Assert.IsTrue(container.Remove<HDRISettings>());
        Assert.AreEqual(1, container.ComponentCount);
        Assert.IsFalse(container.Has<HDRISettings>());
        Assert.IsTrue(container.Has<BloomSettings>());
    }

    [TestMethod]
    public void TestRenderVolumeDataContainer_AlignmentAndMultiComponentStorage()
    {
        var container = RenderVolumeDataContainer.Create();
        try
        {
            // 1. Add EnvironmentSettings (2 bytes, 1 byte alignment)
            container.Set(new EnvironmentSettings { skyType = SkyType.HDRI });

            // 2. Add HDRISettings (48 bytes, 4 or 8 byte alignment)
            var expectedHandle = new Handle<GPUTexture>(15, 2);
            container.Set(new HDRISettings
            {
                texture = expectedHandle,
                intensity = 3.5f,
                rotation = 1.2f,
                tint = new float3(0.8f, 0.9f, 1.0f)
            });

            Assert.AreEqual(2, container.ComponentCount);

            var entries = container.Entries;
            Assert.AreEqual(2, entries.Length);

            var envEntry = entries[0];
            var hdriEntry = entries[1];

            Assert.AreEqual(VolumeBlenderTrampoline<EnvironmentSettings>.TypeId, envEntry.typeId);
            Assert.AreEqual(VolumeBlenderTrampoline<HDRISettings>.TypeId, hdriEntry.typeId);

            // Verify HDRISettings offset is strictly aligned to AlignOf<HDRISettings>()
            var hdriAlign = (int)MemoryUtility.AlignOf<HDRISettings>();
            Assert.AreEqual(0, hdriEntry.offset % hdriAlign, $"HDRISettings offset {hdriEntry.offset} must be aligned to {hdriAlign}");

            // Verify retrieval of both components
            Assert.IsTrue(container.TryGet<EnvironmentSettings>(out var retrievedEnv));
            Assert.AreEqual(SkyType.HDRI, retrievedEnv.skyType.GetValueOrDefault(SkyType.HDRI));

            Assert.IsTrue(container.TryGet<HDRISettings>(out var retrievedHdri));
            Assert.AreEqual(expectedHandle, retrievedHdri.texture!.Value);
            Assert.AreEqual(3.5f, retrievedHdri.intensity!.Value, 1e-4f);
            Assert.AreEqual(1.2f, retrievedHdri.rotation!.Value, 1e-4f);
            Assert.AreEqual(new float3(0.8f, 0.9f, 1.0f), retrievedHdri.tint!.Value);
        }
        finally
        {
            container.Dispose();
        }
    }

    [TestMethod]
    public void TestGhostRenderPayload_ViewVolumeData_StoresAndFallsBackToDefault()
    {
        var payload = new GhostRenderPayload(null!);
        try
        {
            const uint viewId = 1;

            // When unassigned, GetViewVolumeData must return CachedDefault (not zeroed struct)
            ref readonly var defaultSky = ref payload.GetViewVolumeData<HDRISettings>(viewId);
            Assert.AreEqual(1.0f, defaultSky.intensity!.Value);
            Assert.AreEqual(0.0f, defaultSky.rotation!.Value);
            Assert.AreEqual(new float3(1.0f, 1.0f, 1.0f), defaultSky.tint!.Value);

            // Set data for viewId
            var customSky = new HDRISettings
            {
                intensity = 4.2f,
                rotation = 2.1f,
                tint = new float3(0.5f, 0.5f, 1.0f)
            };
            payload.SetViewVolumeData(viewId, in customSky);

            // Now retrieval should return the stored values
            ref readonly var retrievedSky = ref payload.GetViewVolumeData<HDRISettings>(viewId);
            Assert.AreEqual(4.2f, retrievedSky.intensity!.Value, 1e-4f);
            Assert.AreEqual(2.1f, retrievedSky.rotation!.Value, 1e-4f);
            Assert.AreEqual(new float3(0.5f, 0.5f, 1.0f), retrievedSky.tint!.Value);

            // A different viewId must still return CachedDefault
            ref readonly var otherViewSky = ref payload.GetViewVolumeData<HDRISettings>(viewId: 2);
            Assert.AreEqual(1.0f, otherViewSky.intensity!.Value);

            // Reset should clear data and return to default
            payload.Reset();
            ref readonly var resetSky = ref payload.GetViewVolumeData<HDRISettings>(viewId);
            Assert.AreEqual(1.0f, resetSky.intensity!.Value);
        }
        finally
        {
            payload.Dispose();
        }
    }

    [TestMethod]
    public void TestRenderVolumeCleanupSystem_DisposesDyingContainer()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, RenderVolume.DefaultGlobal);

        var container = RenderVolumeDataContainer.Create();
        container.Set(new HDRISettings { intensity = 2.0f });
        _world.EntityManager.AddComponent(entity, container);

        // Entity has both RenderVolume and RenderVolumeDataContainer
        Assert.IsTrue(_world.EntityManager.HasComponent<RenderVolume>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<RenderVolumeDataContainer>(entity));

        // Destroy entity: non-cleanup (RenderVolume) is dropped, cleanup (RenderVolumeDataContainer) survives
        var error = _world.EntityManager.DestroyEntity(entity);
        Assert.AreEqual(Error.None, error);

        Assert.IsTrue(_world.EntityManager.Exists(entity));
        Assert.IsFalse(_world.EntityManager.HasComponent<RenderVolume>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<RenderVolumeDataContainer>(entity));

        // Create and run RenderVolumeCleanupSystem via SystemGroup
        var group = new TestCleanupGroup();

        var api = new SystemAPI { World = _world };
        var updateApi = new SystemUpdateAPI { World = _world };

        group.Initialize(in api);
        group.Update(in updateApi);
        group.Cleanup(in api);

        _world.PlaybackEntityCommandBuffers();

        // Entity should now have its cleanup component removed and cease to exist
        Assert.IsFalse(_world.EntityManager.Exists(entity));
    }

    [TestMethod]
    public void TestRenderVolumeSystem_EvaluatesAndBlendsVolumesIntoPayload()
    {
        // 1. Create Camera entity with viewId = 1
        var cameraEntity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(cameraEntity, new Camera
        {
            renderingLayerMask = RenderingLayerMask.All,
            nearClipPlane = 0.1f,
            farClipPlane = 1000.0f
        });
        _world.EntityManager.AddComponent(cameraEntity, new LocalToWorld
        {
            matrix = float4x4.identity
        });
        _world.EntityManager.AddComponent(cameraEntity, new GPUViewRef
        {
            viewId = 1
        });

        // 2. Create Global Volume with priority 0, weight 1.0, Skybox intensity = 2.0
        var globalVolEntity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(globalVolEntity, new RenderVolume
        {
            renderingLayerMask = RenderingLayerMask.All,
            priority = 0.0f,
            weight = 1.0f,
            isGlobal = true
        });
        var globalContainer = RenderVolumeDataContainer.Create();
        globalContainer.Set(new HDRISettings { intensity = 2.0f });
        globalContainer.Set(new EnvironmentSettings { skyType = SkyType.HDRI });
        _world.EntityManager.AddComponent(globalVolEntity, globalContainer);

        // 3. Create Local Box Volume at (0, 0, 0) with scale 10x10x10, blendDistance = 5.0, priority = 10, Skybox intensity = 6.0
        var localVolEntity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(localVolEntity, new RenderVolume
        {
            renderingLayerMask = RenderingLayerMask.All,
            priority = 10.0f,
            weight = 1.0f,
            isGlobal = false,
            shape = VolumeShape.Box,
            blendDistance = 5.0f
        });
        var localLtw = new float4x4(
            new float4(10.0f, 0.0f, 0.0f, 0.0f),
            new float4(0.0f, 10.0f, 0.0f, 0.0f),
            new float4(0.0f, 0.0f, 10.0f, 0.0f),
            new float4(0.0f, 0.0f, 0.0f, 1.0f)
        );
        _world.EntityManager.AddComponent(localVolEntity, new LocalToWorld { matrix = localLtw });
        var localContainer = RenderVolumeDataContainer.Create();
        localContainer.Set(new HDRISettings { intensity = 6.0f });
        _world.EntityManager.AddComponent(localVolEntity, localContainer);

        // 4. Set up system and payload
        var payload = new GhostRenderPayload(null!);
        try
        {
            var group = new TestVolumeGroup();
            group.VolumeSystem.OverridePayload = payload;

            var api = new SystemAPI { World = _world };
            var updateApi = new SystemUpdateAPI { World = _world };
            group.Initialize(in api);

            // Case A: Camera at (0, 0, 0) inside box volume (half extents = 5)
            // Local volume (priority 10) overrides global volume (priority 0) with full weight -> intensity = 6.0
            group.Update(in updateApi);
            ref readonly var skyInside = ref payload.GetViewVolumeData<HDRISettings>(1);
            Assert.AreEqual(6.0f, skyInside.intensity!.Value, 1e-4f);
            ref readonly var envInside = ref payload.GetViewVolumeData<EnvironmentSettings>(1);
            Assert.IsTrue(envInside.skyType.HasValue);
            Assert.AreEqual(SkyType.HDRI, envInside.skyType.Value);
            Assert.AreEqual(SkyType.HDRI, envInside.skyType.GetValueOrDefault(SkyType.HDRI));

            // Case B: Move camera outside local box volume to (100, 0, 0)
            // Outside falloff (100 > 5 + 5), so local volume weight = 0 -> only global volume affects it -> intensity = 2.0
            ref var camLtw = ref _world.EntityManager.GetComponent<LocalToWorld>(cameraEntity);
            var translated = float4x4.identity;
            translated.c3 = new float4(100.0f, 0.0f, 0.0f, 1.0f);
            camLtw.matrix = translated;

            payload.Reset();
            group.Update(in updateApi);
            ref readonly var skyOutside = ref payload.GetViewVolumeData<HDRISettings>(1);
            Assert.AreEqual(2.0f, skyOutside.intensity!.Value, 1e-4f);

            group.Cleanup(in api);

            // Clean up volume entities via RenderVolumeCleanupSystem
            _world.EntityManager.DestroyEntity(globalVolEntity);
            _world.EntityManager.DestroyEntity(localVolEntity);

            var cleanupGroup = new TestCleanupGroup();
            cleanupGroup.Initialize(in api);
            cleanupGroup.Update(in updateApi);
            cleanupGroup.Cleanup(in api);
            _world.PlaybackEntityCommandBuffers();
        }
        finally
        {
            payload.Dispose();
        }
    }
}
