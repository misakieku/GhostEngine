using Ghost.Core;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Tests;
using Microsoft.UI.Reactor;
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
        GhostDensity.ApplyCompactDensity();

        ReactorApp.Run<StudioApp>("GhostEngine Studio", width: 1440, height: 900);
        return 0;
    }

    private static void SeedSampleScene()
    {
        var entities = new List<EntityNode>();

        // 1. Main Camera
        var camPosProp = new PropertyNode("position", typeof(float3), null!, new float3(12.83f, 6.42f, 18.27f), _ => {});
        var camRotProp = new PropertyNode("rotation", typeof(float3), null!, new float3(0f, 90f, 0f), _ => {});
        var camScaleProp = new PropertyNode("scale", typeof(float3), null!, new float3(1f, 1f, 1f), _ => {});
        var camTransform = new ComponentNode("LocalTransform", typeof(LocalTransform), null!, [camPosProp, camRotProp, camScaleProp]);
        camPosProp.ComponentNode = camTransform;
        camRotProp.ComponentNode = camTransform;
        camScaleProp.ComponentNode = camTransform;
        var mainCamera = new EntityNode(1, "Main Camera", [camTransform]);
        camTransform.EntityNode = mainCamera;
        entities.Add(mainCamera);

        // 2. Directional Light
        var lightPosProp = new PropertyNode("position", typeof(float3), null!, new float3(0f, 100f, 0f), _ => {});
        var lightTransform = new ComponentNode("LocalTransform", typeof(LocalTransform), null!, [lightPosProp]);
        lightPosProp.ComponentNode = lightTransform;
        var dirLight = new EntityNode(2, "Directional Light", [lightTransform]);
        lightTransform.EntityNode = dirLight;
        entities.Add(dirLight);

        // 3. Player Character
        var playerPosProp = new PropertyNode("position", typeof(float3), null!, new float3(0f, 1f, 0f), _ => {});
        var playerScaleProp = new PropertyNode("scale", typeof(float3), null!, new float3(1f, 1f, 1f), _ => {});
        var playerTransform = new ComponentNode("LocalTransform", typeof(LocalTransform), null!, [playerPosProp, playerScaleProp]);
        playerPosProp.ComponentNode = playerTransform;
        playerScaleProp.ComponentNode = playerTransform;
        var playerEntity = new EntityNode(3, "PlayerCharacter", [playerTransform]);
        playerTransform.EntityNode = playerEntity;
        entities.Add(playerEntity);

        var snapshot = new SceneSnapshot(entities, frameTimeMs: 6.9f, fps: 144.0f);
        EditorSyncBridge.Instance.PublishSnapshot(snapshot);
    }
}
