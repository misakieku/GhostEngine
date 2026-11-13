using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Menus;

/// <summary>
/// Engine-specific entity creation presets discovered via <see cref="EditorMenuItemAttribute"/> reflection.
/// </summary>
public static class EngineEntityMenuActions
{
    [EditorMenuItem("Hierarchy", "Create/Camera", Priority = 11, Icon = "\uE722")]
    [EditorMenuItem("Entity", "Create/Camera", Priority = 11, Icon = "\uE722")]
    public static void CreateCamera(MenuContext ctx)
    {
        var cmd = new CreateEntityCommand("Camera", ctx.TargetEntity?.Id, new Type[] { typeof(LocalTransform), typeof(Camera) }, ctx.Bridge, ctx.Selection);
        ctx.History.Execute(cmd);
    }

    [EditorMenuItem("Hierarchy", "Create/Mesh", Priority = 12, Icon = "\uE7C3")]
    [EditorMenuItem("Entity", "Create/Mesh", Priority = 12, Icon = "\uE7C3")]
    public static void CreateMesh(MenuContext ctx)
    {
        var cmd = new CreateEntityCommand("Mesh", ctx.TargetEntity?.Id, new Type[] { typeof(LocalTransform), typeof(MeshInstance) }, ctx.Bridge, ctx.Selection);
        ctx.History.Execute(cmd);
    }

    [EditorMenuItem("Hierarchy", "Create/Light/Directional Light", Priority = 13, Icon = "\uE706")]
    [EditorMenuItem("Entity", "Create/Light/Directional Light", Priority = 13, Icon = "\uE706")]
    public static void CreateDirectionalLight(MenuContext ctx)
    {
        var cmd = new CreateEntityCommand("Directional Light", ctx.TargetEntity?.Id, new Type[] { typeof(LocalTransform), typeof(DirectionalLight) }, ctx.Bridge, ctx.Selection);
        ctx.History.Execute(cmd);
    }

    [EditorMenuItem("Hierarchy", "Create/Light/Point Light", Priority = 14, Icon = "\uEA80")]
    [EditorMenuItem("Entity", "Create/Light/Point Light", Priority = 14, Icon = "\uEA80")]
    public static void CreatePointLight(MenuContext ctx)
    {
        CreateLightEntity(ctx, "Point Light", PunctualLight.CreatePoint(new float3(1f, 1f, 1f), 5.0f, 10.0f));
    }

    [EditorMenuItem("Hierarchy", "Create/Light/Spot Light", Priority = 15, Icon = "\uEA80")]
    [EditorMenuItem("Entity", "Create/Light/Spot Light", Priority = 15, Icon = "\uEA80")]
    public static void CreateSpotLight(MenuContext ctx)
    {
        CreateLightEntity(ctx, "Spot Light", PunctualLight.CreateSpot(new float3(1f, 1f, 1f), 8.0f, 15.0f, 0.35f, 0.7f));
    }

    private static void CreateLightEntity(MenuContext ctx, string name, PunctualLight light)
    {
        var entity = new EntityNode(ctx.Bridge.AllocateEntityId(), name, new List<ComponentNode>(), ctx.TargetEntity?.Id);
        var comps = new List<ComponentNode>(2);
        comps.Add(ComponentNodeFactory.CreateComponentNode(typeof(LocalTransform), entity, LocalTransform.Identity, ctx.Bridge));
        comps.Add(ComponentNodeFactory.CreateComponentNode(typeof(PunctualLight), entity, light, ctx.Bridge));
        entity.Components = comps;
        ctx.History.Execute(new CreateEntityCommand(entity, ctx.Bridge, ctx.Selection));
    }
}
