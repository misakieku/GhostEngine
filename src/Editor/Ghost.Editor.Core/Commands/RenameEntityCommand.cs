using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that renames an <see cref="EntityNode"/> and republishes the scene snapshot.
/// </summary>
public sealed class RenameEntityCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _target;
    private readonly string _oldName;
    private readonly string _newName;

    public string Name => $"Rename {_oldName} to {_newName}";

    public RenameEntityCommand(EntityNode target, string newName, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _target = target;
        _oldName = target.Name;
        _newName = string.IsNullOrWhiteSpace(newName) ? target.Name : newName.Trim();
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
    }

    public void Execute() => ApplyName(_newName);

    public void Undo() => ApplyName(_oldName);

    public void Redo() => ApplyName(_newName);

    private void ApplyName(string name)
    {
        if (string.Equals(_target.Name, name, StringComparison.Ordinal))
        {
            return;
        }

        _target.Name = name;
        _bridge.MutateSnapshot(entities =>
        {
            var next = new List<EntityNode>(entities.Count);
            for (var i = 0; i < entities.Count; i++)
            {
                next.Add(entities[i]);
            }

            return next;
        });

        _selection.Select(_target);
    }
}
