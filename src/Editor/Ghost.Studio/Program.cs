using Ghost.Core;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Tests;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Docking.Native;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--headless-test")
        {
            return StudioHeadlessRunner.Run();
        }

        SeedSampleScene();

        ReactorApp.Run<StudioApp>("GhostEngine Studio", width: 1440, height: 900, configure: host =>
        {
            DockingNativeInterop.Register(host.Reconciler);
            GhostDensity.ApplyCompactDensity();
        });
        return 0;
    }

    private static void SeedSampleScene()
    {
        var bridge = EditorSyncBridge.Instance;
        var entities = new List<EntityNode>(3);

        var mainCamera = CreateSeedEntity(bridge, "Main Camera", LocalTransform.Identity with { position = new float3(12.83f, 6.42f, 18.27f) }, typeof(Camera), null);
        entities.Add(mainCamera);

        var dirLight = CreateSeedEntity(bridge, "Directional Light", LocalTransform.Identity with { position = new float3(0f, 100f, 0f) }, typeof(DirectionalLight), DirectionalLight.Default);
        entities.Add(dirLight);

        var playerEntity = CreateSeedEntity(bridge, "PlayerCharacter", LocalTransform.Identity with { position = new float3(0f, 1f, 0f) }, typeof(MeshInstance), null);
        entities.Add(playerEntity);

        var snapshot = new SceneSnapshot(entities, frameTimeMs: 6.9f, fps: 144.0f);
        bridge.PublishSnapshot(snapshot);
    }

    private static EntityNode CreateSeedEntity(EditorSyncBridge bridge, string name, LocalTransform transform, Type extraComponentType, object? extraValue)
    {
        var entity = new EntityNode(bridge.AllocateEntityId(), name, new List<ComponentNode>(2));
        var comps = new List<ComponentNode>(2);
        comps.Add(ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), entity, transform, bridge));
        comps.Add(ComponentNodeFactory.CreateComponentNode(extraComponentType, entity, extraValue, bridge));
        entity.Components = comps;
        return entity;
    }
}
