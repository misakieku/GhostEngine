using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Input;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using TestGame.Systems;

namespace TestGame;

internal class LaunchProfile : IEngineLanunchProfile
{
    private World _world = null!;
    private IAssetEntry _meshAsset = null!;
    private IAssetEntry _shaderAsset = null!;
    private IAssetEntry _shaderAsset2 = null!;

    private Entity _camera;

    private readonly GhostRenderPipelineSettings _renderPipelineSettings = new GhostRenderPipelineSettings
    {
        MaxVisibleMeshletsOnScreen = 2_097_152,
        MeshletLodErrorThreshold = 2.0f,
        InstanceCullingThreshold = 2.0f,
    };

    public EngineDesc GetEngineDesc()
    {
        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc
            {
                Width = 2560,
                Height = 1440,
                Title = "TestGame"
            },
            JobSchedulerDesc = new JobSchedulerDesc
            {
                ThreadCount = Environment.ProcessorCount - 2,
                ThreadPriority = ThreadPriority.Normal,
                DependencyChainCapacity = 8192,
            }
        };
    }

    public GraphicsDesc GetGraphicsDesc()
    {
        IGraphicsEngine graphicsEngine;
#if DEBUG
        var enableDebugLayer = true;
#else
        var enableDebugLayer = false;
#endif

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
        {
            graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc(), enableDebugLayer);
        }
        else
        {
            throw new PlatformNotSupportedException("Direct3D 12 is only supported on Windows 10 version 19041 or later.");
        }

        return new GraphicsDesc
        {
            FrameBufferCount = 2,
            GraphicsEngine = graphicsEngine,
            RenderPipelineSettings = _renderPipelineSettings,
        };
    }

    public IContentProvider GetContentProvider()
    {
        return new RuntimeContentProvider("Assets/manifest.json");
    }

    private static float RandomFloat(float min, float max)
    {
        return (float)(Random.Shared.NextSingle() * (max - min) + min);
    }

    public void OnEngineInitialized(EngineCore engine)
    {
        const int entityCapacity = 10000;
        const float size = 20.0f;
        const float baseScale = 0.5f;

        _world = World.Create(engine.JobScheduler, entityCapacity);

        using var scope = AllocationManager.CreateStackScope();
        _camera = _world.EntityManager.CreateEntity(
            new Camera
            {
                swapChainIndex = 0,
                depthTarget = Handle<GPUTexture>.Invalid,
                nearClipPlane = 0.1f,
                farClipPlane = 1000.0f,
                focalLength = 20.0f,
                sensorSize = new float2(36.0f, 24.0f),
                gateFit = GateFit.Vertical,
                renderingLayerMask = RenderingLayerMask.All,
            },
            new LocalToWorld
            {
                matrix = float4x4.TRS(new float3(0.0f, 0.0f, -10.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
            },
            FirstPersonCamera.Default,
            new InputReceiver(InputProfileDatabase.FIRST_PERSON_CAMERA_PROFILE_ID, true),
            default(ActionState));

        _meshAsset = engine.AssetManager.ResolveAsset("Meshes/dragon");
        _shaderAsset = engine.AssetManager.ResolveAsset("Shaders/test");
        _shaderAsset2 = engine.AssetManager.ResolveAsset("Shaders/test2");

        var meshHandle = default(Handle<Mesh>);
        _meshAsset.ReadAssetData(ref meshHandle);

        var shaderHandle = default(Handle<Shader>);
        _shaderAsset.ReadAssetData(ref shaderHandle);
        var shaderHandle2 = default(Handle<Shader>);
        _shaderAsset2.ReadAssetData(ref shaderHandle2);

        var mat = engine.RenderEngine.ResourceManager.CreateMaterial(shaderHandle);
        var mat2 = engine.RenderEngine.ResourceManager.CreateMaterial(shaderHandle2);
        var materialPallette = engine.RenderEngine.ResourceManager.GetOrCreateMaterialPalette([mat]);
        var materialPallette2 = engine.RenderEngine.ResourceManager.GetOrCreateMaterialPalette([mat2]);

        using var meshSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<MeshInstance>.Value, ComponentTypeID<LocalToWorld>.Value);
        var entities = new Entity[entityCapacity];
        _world.EntityManager.CreateEntities(entities, meshSet);

        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            _world.EntityManager.SetComponent(entity, new MeshInstance
            {
                mesh = meshHandle,
                materialPalette = i % 2 == 0 ? materialPallette : materialPallette2,
                renderingLayerMask = RenderingLayerMask.All,
                shadowCastingMode = ShadowCastingMode.On,
                staticShadowCaster = true,
            });

            var position = new float3(RandomFloat(-size, size), RandomFloat(-size, size), RandomFloat(-size, size));
            var rotation = quaternion.EulerXYZ(new float3(RandomFloat(0.0f, 360.0f), RandomFloat(0.0f, 360.0f), RandomFloat(0.0f, 360.0f)));
            var scale = new float3(RandomFloat(baseScale, baseScale * 2.0f), RandomFloat(baseScale, baseScale * 2.0f), RandomFloat(baseScale, baseScale * 2.0f));

            _world.EntityManager.SetComponent(entity, new LocalToWorld
            {
                matrix = float4x4.TRS(position, rotation, scale)
            });
        }

        var profileDb = new InputProfileDatabase();
        _world.AddService(profileDb);

        _world.SystemManager.AddSystem<InputEvaluationSystem>();
        _world.SystemManager.AddSystem<FirstPersonCameraSystem>();
        _world.SystemManager.AddSystem<RenderSystemGroup>();

        _world.AddService(engine.RenderEngine);

        engine.InputManager.RelativeMouseMode = true;
    }

    public void OnEngineShutdown(EngineCore engine)
    {
        World.Destroy(_world);

        _meshAsset.Release();
        _shaderAsset.Release();
        _shaderAsset2.Release();
    }
}
