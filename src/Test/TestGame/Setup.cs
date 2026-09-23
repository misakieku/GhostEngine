using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
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
using SDL;

namespace TestGame;

internal static class Setup
{
    private static World s_world = null!;
    private static IAssetEntry s_meshAsset = null!;
    private static IAssetEntry s_shaderAsset = null!;
    private static Entity s_camera;

    private static readonly GhostRenderPipelineSettings s_renderPipelineSettings = new GhostRenderPipelineSettings
    {
        MaxVisibleMeshletsOnScreen = 2_097_152 * 1,
        MeshletLodErrorThreshold = 1.0f,
        InstanceCullingThreshold = 2.0f,
    };

    [RuntimeConfiguration]
    public static EngineDesc InitEngineDesc()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        return new EngineDesc
        {
            AllocationManagerDesc = AllocationManagerDesc.Default,
            WindowDesc = new WindowDesc { Width = 2560, Height = 1440, Title = "TestGame" },
            JobSchedulerDesc = new JobSchedulerDesc
            {
                ThreadCount = Environment.ProcessorCount - 2,
                ThreadPriority = ThreadPriority.Normal,
                DependencyChainCapacity = 8192,
            },
            RenderDescFactory = static () => new EngineDesc.Render
            {
                FrameBufferCount = 2,
                GraphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc { FrameBufferCount = 2 },
#if DEBUG
                true
#else
                false
#endif
                ),
                RenderPipelineSettings = s_renderPipelineSettings,
                ShaderCacheDirectory = "ShaderCache",
                ShaderCompilationBridge = null
            },
            ContentProviderFactory = static () => new RuntimeContentProvider("Assets/manifest.json")
        };
#pragma warning restore CA1416 // Validate platform compatibility
    }

    private static float RandomFloat(float min, float max)
    {
        return (float)(Random.Shared.NextSingle() * (max - min) + min);
    }

    [RuntimeInitialize]
    public static void Init(EngineCore engineCore)
    {
        EngineWindow.OnEvent += e =>
        {
            // Change debug mode in settings using F1, F2, F3, etc. keys
            if (e.type == (uint)SDL_EventType.SDL_EVENT_KEY_DOWN)
            {
                const float delta = 0.1f;
                ref var matrix = ref s_world.EntityManager.GetComponent<LocalToWorld>(s_camera).matrix;
                MathUtility.GetTRS(matrix, out var position, out var rotation, out var scale);

                switch (e.key.key)
                {
                    case SDL_Keycode.SDLK_W:
                        matrix = float4x4.TRS(position + new float3(0.0f, 0.0f, delta), quaternion.identity, new float3(1.0f, 1.0f, 1.0f));
                        break;
                    case SDL_Keycode.SDLK_S:
                        matrix = float4x4.TRS(position - new float3(0.0f, 0.0f, delta), quaternion.identity, new float3(1.0f, 1.0f, 1.0f));
                        break;
                    case SDL_Keycode.SDLK_A:
                        matrix = float4x4.TRS(position - new float3(delta, 0.0f, 0.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f));
                        break;
                    case SDL_Keycode.SDLK_D:
                        matrix = float4x4.TRS(position + new float3(delta, 0.0f, 0.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f));
                        break;
                    case SDL_Keycode.SDLK_Q:
                        matrix = float4x4.TRS(position + new float3(0.0f, delta, 0.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f));
                        break;
                    case SDL_Keycode.SDLK_E:
                        matrix = float4x4.TRS(position - new float3(0.0f, delta, 0.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f));
                        break;
                }
            }
        };

        const int entityCapacity = 100;
        const float size = 5.0f;
        const float baseScale = 3.0f;

        s_world = World.Create(engineCore.JobScheduler, entityCapacity);

        using var scope = AllocationManager.CreateStackScope();
        using var camSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Camera>.Value, ComponentTypeID<LocalToWorld>.Value, ComponentTypeID<MoveDst>.Value);
        s_camera = s_world.EntityManager.CreateEntity(camSet);

        s_world.EntityManager.SetComponent(s_camera, new Camera
        {
            swapChainIndex = 0,
            depthTarget = Handle<GPUTexture>.Invalid,
            nearClipPlane = 0.1f,
            farClipPlane = 1000.0f,
            focalLength = 20.0f,
            sensorSize = new float2(36.0f, 24.0f),
            gateFit = GateFit.Vertical,
            renderingLayerMask = RenderingLayerMask.All,
        });

        s_world.EntityManager.SetComponent(s_camera, new LocalToWorld
        {
            matrix = float4x4.TRS(new float3(0.0f, 0.0f, -10.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
        });

        s_world.EntityManager.SetComponent(s_camera, new MoveDst
        {
            position = new float3(0.0f, 0.0f, -20.0f),
            lookAt = float3.zero,
            range = new float3(20.0f, 20.0f, 20.0f),
            updateRotation = true
        });

        s_meshAsset = engineCore.AssetManager.ResolveAsset("Meshes/dragon");
        s_shaderAsset = engineCore.AssetManager.ResolveAsset("Shaders/test");

        var meshHandle = default(Handle<Mesh>);
        s_meshAsset.ReadAssetData(ref meshHandle);

        var shaderHandle = default(Handle<Shader>);
        s_shaderAsset.ReadAssetData(ref shaderHandle);

        // TODO: Create material from shader
        var mat = engineCore.RenderEngine.ResourceManager.CreateMaterial(shaderHandle);
        var materialPallette = engineCore.RenderEngine.ResourceManager.GetOrCreateMaterialPalette([mat]);

        using var meshSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<MeshInstance>.Value, ComponentTypeID<LocalToWorld>.Value);
        var entities = new Entity[entityCapacity];
        s_world.EntityManager.CreateEntities(entities, meshSet);

        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            s_world.EntityManager.SetComponent(entity, new MeshInstance
            {
                mesh = meshHandle,
                materialPalette = materialPallette,
                renderingLayerMask = RenderingLayerMask.All,
                shadowCastingMode = ShadowCastingMode.On,
                staticShadowCaster = true,
            });

            var position = new float3(RandomFloat(-size, size), RandomFloat(-size, size), RandomFloat(-size, size));
            var rotation = quaternion.EulerXYZ(new float3(RandomFloat(0.0f, 360.0f), RandomFloat(0.0f, 360.0f), RandomFloat(0.0f, 360.0f)));
            // var scale = new float3(RandomFloat(baseScale, baseScale * 2.0f), RandomFloat(baseScale, baseScale * 2.0f), RandomFloat(baseScale, baseScale * 2.0f));
            var scale = float3.one * 3.0f;

            s_world.EntityManager.SetComponent(entity, new LocalToWorld
            {
                matrix = float4x4.TRS(position, rotation, scale)
            });
        }

        //var defaultSystemGroup = new DefaultSystemGroup();
        //defaultSystemGroup.AddSystem<Systems.RandomMoveSystem>();
        //defaultSystemGroup.SortSystems();

        //s_world.SystemManager.AddSystem(defaultSystemGroup);
        s_world.SystemManager.AddSystem<RenderSystemGroup>();

        s_world.AddService(engineCore.RenderEngine);
    }

    [RuntimeShutdown]
    public static void Shutdown(EngineCore engineCore)
    {
        World.Destroy(s_world);

        s_meshAsset.Release();
        s_shaderAsset.Release();
    }
}
