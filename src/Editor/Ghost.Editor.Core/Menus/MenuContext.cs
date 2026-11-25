using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Menus;

/// <summary>
/// Execution and visibility context passed to editor menu items and validators.
/// </summary>
public sealed class MenuContext
{
    public EditorSyncBridge Bridge { get; }
    public CommandHistory History { get; }
    public EditorSelection Selection { get; }
    public EntityNode? TargetEntity { get; }
    public object? UserData { get; }

    public EntityNode? EffectiveEntity => TargetEntity ?? Selection.ActiveEntity;

    public MenuContext(EntityNode? targetEntity = null, EditorSyncBridge? bridge = null, CommandHistory? history = null, EditorSelection? selection = null, object? userData = null)
    {
        TargetEntity = targetEntity;
        Bridge = bridge ?? EditorSyncBridge.Instance;
        History = history ?? CommandHistory.Instance;
        Selection = selection ?? EditorSelection.Instance;
        UserData = userData;
    }
}
