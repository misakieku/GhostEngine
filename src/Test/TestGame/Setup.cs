using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace TestGame;

internal static class Setup
{
    private static World _world = null!;

    [RuntimeInitialize]
    public static void Init(EngineCore engineCore)
    {
        _world = World.Create(engineCore.JobScheduler, 1024);

        using var scope = AllocationManager.CreateStackScope();
        var camSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Camera>.Value, ComponentTypeID<LocalToWorld>.Value);
        var cameraEntity = _world.EntityManager.CreateEntity(camSet);

        _world.EntityManager.SetComponent(cameraEntity, new Camera
        {
            swapChainIndex = 0,
            depthTarget = Handle<GPUTexture>.Invalid,
            nearClipPlane = 0.1f,
            farClipPlane = 1000.0f,
            focalLength = 50.0f,
            sensorSize = new float2(36.0f, 24.0f),
            gateFit = GateFit.Vertical,
            renderingLayerMask = RenderingLayerMask.All,
        });

        _world.EntityManager.SetComponent(cameraEntity, new LocalToWorld
        {
            matrix = float4x4.TRS(new float3(0.0f, 0.0f, -5.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
        });

        _world.SystemManager.AddSystem<RenderSystemGroup>();
    }

    [RuntimeShutdown]
    public static void Shutdown(EngineCore engineCore)
    {
        World.Destroy(_world);
    }
}
