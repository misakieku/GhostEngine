using Ghost.Core;
using Ghost.Core.Collections;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;

namespace Ghost.Editor.Core.Services;

public enum LifecycleEvent { Created, Destroyed }

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

public class ObjectStateOperation : UndoOperation
{
    public Guid InstanceID { get; set; }
    public byte[] State { get; set; } = Array.Empty<byte>();

    public override UndoOperation CreateReciprocal(IEditorWorldService worldService)
    {
        var obj = GhostObject.Find(InstanceID);
        var reciprocal = new ObjectStateOperation { GroupId = GroupId, ActionName = ActionName, InstanceID = InstanceID };
        if (obj != null)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            obj.SerializeState(writer);
            reciprocal.State = ms.ToArray();
        }
        return reciprocal;
    }

    public override void Revert(IEditorWorldService worldService)
    {
        var obj = GhostObject.Find(InstanceID);
        if (obj != null)
        {
            using var ms = new MemoryStream(State);
            using var reader = new BinaryReader(ms);
            obj.DeserializeState(reader);
        }
    }

    public override bool CanMerge(UndoOperation other)
    {
        if (other is ObjectStateOperation op)
        {
            return op.InstanceID == InstanceID && op.GroupId == GroupId;
        }
        return false;
    }
}


internal class UndoService : IUndoService
{
    public event Action? UndoRedoPerformed;

    private readonly IEditorWorldService _worldService;
    private readonly RingBuffer<UndoOperation> _undoStack = new(50);
    private readonly Stack<UndoOperation> _redoStack = new();

    private int _nextGroupId = 1;
    private int _activeGroupId = 0;

    public int GlobalVersion { get; private set; } = 0;

    public IEnumerable<UndoOperation> UndoOperations => _undoStack;
    public IEnumerable<UndoOperation> RedoOperations => _redoStack;

    public UndoService(IEditorWorldService worldService)
    {
        _worldService = worldService;
    }

    public void BeginTransaction(string name)
    {
        _activeGroupId = _nextGroupId++;
    }

    public void EndTransaction()
    {
        _activeGroupId = 0;
    }

    private void PushOperation(UndoOperation op)
    {
        bool isTransaction = _activeGroupId != 0;
        op.GroupId = isTransaction ? _activeGroupId : 0;

        UndoOperation? top = _undoStack.Count > 0 ? _undoStack.Peek() : null;
        if (top != null && op.CanMerge(top))
        {
            // Extend the merge window by updating the timestamp
            top.Timestamp = op.Timestamp;
            return;
        }

        if (!isTransaction)
        {
            op.GroupId = _nextGroupId++;
        }

        _undoStack.Push(op);
        _redoStack.Clear(); // Any new action clears the redo stack
        GlobalVersion++;
    }

    public void RecordObject(GhostObject obj, string actionName)
    {
        var op = new ObjectStateOperation()
        {
            ActionName = actionName,
            InstanceID = obj.InstanceID
        };
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        obj.SerializeState(writer);
        op.State = ms.ToArray();
        PushOperation(op);
    }



    public void RegisterCreatedObjectUndo(GhostObject obj, string actionName)
    {
        var op = new ObjectStateOperation()
        {
            ActionName = actionName,
            InstanceID = obj.InstanceID
        };
        // The object is created, so before its creation, it did NOT exist.
        // We write a manual state payload indicating it does not exist.
        // During Undo, DeserializeState will read this and destroy the object.
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        if (obj is SceneGraph.SceneGraphNode sgn)
        {
            writer.Write(sgn.Name);
        }
        else
        {
            writer.Write("");
        }
        writer.Write(false); // IsAlive = false
        op.State = ms.ToArray();
        PushOperation(op);
    }

    public void PerformUndo()
    {
        if (_undoStack.Count == 0)
        {
            return;
        }

        var targetGroup = _undoStack.Peek().GroupId;
        var toUndo = new List<UndoOperation>();

        while (_undoStack.Count > 0 && _undoStack.Peek().GroupId == targetGroup)
        {
            toUndo.Add(_undoStack.Pop());
        }

        var toRedo = new List<UndoOperation>();

        // Revert in reverse order (which is standard for stack pop, but we popped them into a list)
        // Wait, the list has them in reverse chronological order (newest at index 0).
        // We should execute them in that order.
        foreach (var op in toUndo)
        {
            // Snapshot current state for Redo BEFORE reverting
            var reciprocal = op.CreateReciprocal(_worldService);
            toRedo.Add(reciprocal);

            op.Revert(_worldService);
        }

        // Push to Redo stack (we push the oldest action first so it comes off last on redo)
        toRedo.Reverse();
        foreach (var op in toRedo)
        {
            _redoStack.Push(op);
        }

        GlobalVersion--;

        // Flush ECS commands before UI updates
        _worldService.FlushCommands();

        UndoRedoPerformed?.Invoke();
    }

    public void PerformRedo()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var targetGroup = _redoStack.Peek().GroupId;
        var toRedo = new List<UndoOperation>();

        while (_redoStack.Count > 0 && _redoStack.Peek().GroupId == targetGroup)
        {
            toRedo.Add(_redoStack.Pop());
        }

        toRedo.Reverse();

        var toUndo = new List<UndoOperation>();

        foreach (var op in toRedo)
        {
            var reciprocal = op.CreateReciprocal(_worldService);
            toUndo.Add(reciprocal);
            op.Revert(_worldService); // Revert actually means Apply in this symmetric design
        }

        foreach (var op in toUndo)
        {
            _undoStack.Push(op);
        }

        GlobalVersion++;

        _worldService.FlushCommands();

        UndoRedoPerformed?.Invoke();
    }
}
