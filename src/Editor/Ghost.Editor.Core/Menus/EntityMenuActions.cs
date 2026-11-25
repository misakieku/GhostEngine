using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Menus;

/// <summary>
/// Built-in entity hierarchy and lifecycle actions discovered by <see cref="EditorMenuRegistry"/>.
/// </summary>
public static class EntityMenuActions
{
    [EditorMenuItem("Hierarchy", "Create/Empty Entity", Priority = 10, Icon = "\uE710")]
    [EditorMenuItem("Entity", "Create/Empty Entity", Priority = 10, Icon = "\uE710")]
    public static void CreateEmptyEntity(MenuContext ctx)
    {
        var parentId = ctx.TargetEntity?.Id;
        var cmd = new CreateEntityCommand("Empty Entity", parentId, null, ctx.Bridge, ctx.Selection);
        ctx.History.Execute(cmd);
    }

    [EditorMenuItem("Hierarchy", "Rename", Priority = 25, Icon = "\uE8AC", Shortcut = "F2", ValidateMethod = nameof(HasTargetEntity))]
    [EditorMenuItem("Entity", "Rename", Priority = 25, Icon = "\uE8AC", Shortcut = "F2", ValidateMethod = nameof(HasEffectiveEntity))]
    public static void RenameEntity(MenuContext ctx)
    {
        var target = ctx.EffectiveEntity;
        if (target == null)
        {
            return;
        }

        ctx.Selection.RequestRename(target);
    }

    [EditorMenuItem("Hierarchy", "Duplicate", Priority = 30, Shortcut = "Ctrl+D", ValidateMethod = nameof(HasTargetEntity))]
    [EditorMenuItem("Entity", "Duplicate", Priority = 30, Shortcut = "Ctrl+D", ValidateMethod = nameof(HasEffectiveEntity))]
    public static void DuplicateEntity(MenuContext ctx)
    {
        var source = ctx.EffectiveEntity;
        if (source == null)
        {
            return;
        }

        var newId = ctx.Bridge.AllocateEntityId();
        var clone = ComponentNodeFactory.CloneEntityNode(source, newId, $"{source.Name} (Copy)", source.ParentId, ctx.Bridge);
        ctx.History.Execute(new CreateEntityCommand(clone, ctx.Bridge, ctx.Selection));
    }

    [EditorMenuItem("Hierarchy", "Unparent", Priority = 35, ValidateMethod = nameof(HasTargetParentEntity))]
    [EditorMenuItem("Entity", "Unparent", Priority = 35, ValidateMethod = nameof(HasEffectiveParentEntity))]
    public static void UnparentEntity(MenuContext ctx)
    {
        var target = ctx.EffectiveEntity;
        if (target == null || !target.ParentId.HasValue)
        {
            return;
        }

        ctx.History.Execute(new ReparentEntityCommand(target, null, ctx.Bridge, ctx.Selection));
    }

    [EditorMenuItem("Hierarchy", "Delete", Priority = 50, Icon = "\uE74D", Shortcut = "Del", ValidateMethod = nameof(HasTargetEntity))]
    [EditorMenuItem("Entity", "Delete", Priority = 50, Icon = "\uE74D", Shortcut = "Del", ValidateMethod = nameof(HasEffectiveEntity))]
    public static void DeleteEntity(MenuContext ctx)
    {
        var target = ctx.EffectiveEntity;
        if (target == null)
        {
            return;
        }

        ctx.History.Execute(new DeleteEntityCommand(target, ctx.Bridge, ctx.Selection));
    }

    private static bool HasTargetEntity(MenuContext ctx) => ctx.TargetEntity != null;

    private static bool HasEffectiveEntity(MenuContext ctx) => ctx.EffectiveEntity != null;

    private static bool HasTargetParentEntity(MenuContext ctx) => ctx.TargetEntity?.ParentId != null;

    private static bool HasEffectiveParentEntity(MenuContext ctx) => ctx.EffectiveEntity?.ParentId != null;
}
