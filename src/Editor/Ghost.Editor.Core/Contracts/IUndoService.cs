using Ghost.Editor.Core.Services;

namespace Ghost.Editor.Core.Contracts;

public abstract class UndoOperation
{
    public int GroupId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Creates an operation that holds the *current* state, so it can be pushed to Redo.
    public abstract UndoOperation CreateReciprocal(IEditorWorldService worldService);
    public abstract void Revert(IEditorWorldService worldService);

    public virtual bool CanMerge(UndoOperation other) => false;
}

public interface IUndoService
{
    IEnumerable<UndoOperation> UndoOperations { get; }
    IEnumerable<UndoOperation> RedoOperations { get; }

    int GlobalVersion { get; }

    event Action? UndoRedoPerformed;

    void RecordObject(GhostObject obj, string actionName);
    void RegisterCreatedObjectUndo(GhostObject obj, string actionName);

    void BeginTransaction(string name);
    void EndTransaction();
    void PerformUndo();
    void PerformRedo();
}