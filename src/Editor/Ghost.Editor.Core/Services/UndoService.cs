using Ghost.Core;
using Ghost.Core.Collections;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Entities;

namespace Ghost.Editor.Core.Services;

public enum LifecycleEvent { Created, Destroyed }

public interface IUndoService
{
    event Action? UndoRedoPerformed;

    void RecordObject(GhostObject obj, string actionName);
    void RecordEntityComponent(ComponentNode node, string actionName);
    void RecordEntityStructure(EntityNode node, string actionName);
    void RecordEntityLifecycle(EntityNode node, LifecycleEvent type);

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

public class EntityComponentOperation : UndoOperation
{
    public Guid InstanceID { get; set; }
    public Entity Entity { get; set; }
    public int ComponentId { get; set; }
    public byte[] ComponentData { get; set; } = Array.Empty<byte>();

    public override UndoOperation CreateReciprocal(IEditorWorldService worldService)
    {
        var node = GhostObject.Find(InstanceID) as EntityNode;
        var targetEntity = node?.Entity ?? Entity;

        var reciprocal = new EntityComponentOperation { GroupId = GroupId, ActionName = ActionName, Entity = targetEntity, InstanceID = InstanceID, ComponentId = ComponentId };
        unsafe
        {
            var pComp = worldService.EditorWorld.EntityManager.GetComponent(targetEntity, new Identifier<IComponent>(ComponentId));
            if (pComp != null)
            {
                var size = ComponentRegistry.GetComponentInfo(new Identifier<IComponent>(ComponentId)).size;
                var data = new byte[size];
                fixed (byte* pDst = data)
                {
                    Buffer.MemoryCopy(pComp, pDst, size, size);
                }
                reciprocal.ComponentData = data;
            }
        }
        return reciprocal;
    }

    public override void Revert(IEditorWorldService worldService)
    {
        var cId = ComponentId;
        var data = ComponentData;
        var instId = InstanceID;
        var fallbackEntity = Entity;

        worldService.Defer(() =>
        {
            var node = GhostObject.Find(instId) as EntityNode;
            var targetEntity = node?.Entity ?? fallbackEntity;

            unsafe
            {
                var pComp = worldService.EditorWorld.EntityManager.GetComponent(targetEntity, new Identifier<IComponent>(cId));
                if (pComp != null)
                {
                    fixed (byte* pSrc = data)
                    {
                        Buffer.MemoryCopy(pSrc, pComp, data.Length, data.Length);
                    }
                }
            }
        });
    }

    public override bool CanMerge(UndoOperation other)
    {
        if (other is EntityComponentOperation op)
        {
            if (op.Entity == Entity && op.ComponentId == ComponentId)
            {
                // Explicit transaction merge
                if (op.GroupId != 0 && op.GroupId == GroupId)
                {
                    return true;
                }
                
                // Time-based merge fallback for non-transactional continuous edits (e.g. 500ms)
                if (op.GroupId == 0)
                {
                    return Math.Abs((op.Timestamp - Timestamp).TotalMilliseconds) < 500;
                }
            }
        }
        return false;
    }
}

public class EntityStructureOperation : UndoOperation
{
    public Guid InstanceID { get; set; }
    public Entity Entity { get; set; }
    public int ArchetypeID { get; set; }
    public byte[] ComponentData { get; set; } = Array.Empty<byte>();
    public byte[] SharedData { get; set; } = Array.Empty<byte>();
    public int SharedDataHash { get; set; }

    public static EntityStructureOperation Capture(IEditorWorldService worldService, EntityNode node)
    {
        var entity = node.Entity;
        var op = new EntityStructureOperation { Entity = entity, InstanceID = node.InstanceID };
        var locRes = worldService.EditorWorld.EntityManager.GetEntityLocation(entity);
        if (locRes.IsSuccess)
        {
            op.ArchetypeID = locRes.Value.archetypeID;
            unsafe
            {
                ref var archetype = ref worldService.EditorWorld.ComponentManager.GetArchetypeReference(op.ArchetypeID);
                ref var chunk = ref archetype.GetChunkReference(locRes.Value.chunkIndex);

                // Compute size of all unmanaged components
                var totalSize = 0;
                for (var i = 0; i < archetype._layouts.Count; i++)
                {
                    totalSize += archetype._layouts[i].size;
                }

                var data = new byte[totalSize];
                fixed (byte* pDst = data)
                {
                    var offset = 0;
                    for (var i = 0; i < archetype._layouts.Count; i++)
                    {
                        var layout = archetype._layouts[i];
                        var pSrc = chunk.GetUnsafePtr() + layout.offset + (layout.size * locRes.Value.rowIndex);
                        Buffer.MemoryCopy(pSrc, pDst + offset, layout.size, layout.size);
                        offset += layout.size;
                    }
                }
                op.ComponentData = data;

                if (chunk._groupIndex >= 0 && chunk._groupIndex < archetype._chunkGroups.Count)
                {
                    var group = archetype._chunkGroups[chunk._groupIndex];
                    op.SharedData = group.sharedData.AsSpan().ToArray();
                    op.SharedDataHash = group.sharedDataHash;
                }
            }
        }
        return op;
    }

    public override UndoOperation CreateReciprocal(IEditorWorldService worldService)
    {
        if (GhostObject.Find(InstanceID) is not EntityNode node)
        {
            return this;
        }

        var reciprocal = Capture(worldService, node);
        reciprocal.GroupId = GroupId;
        reciprocal.ActionName = ActionName;
        return reciprocal;
    }

    public override void Revert(IEditorWorldService worldService)
    {
        var instId = InstanceID;
        var fallbackEntity = Entity;
        var archId = ArchetypeID;
        var compData = ComponentData;
        var sharedData = SharedData;
        var sharedHash = SharedDataHash;

        worldService.Defer(() =>
        {
            var node = GhostObject.Find(instId) as EntityNode;
            var targetEntity = node?.Entity ?? fallbackEntity;

            var world = worldService.EditorWorld;
            var locRes = world.EntityManager.GetEntityLocation(targetEntity);
            if (!locRes.IsSuccess)
            {
                return; // Entity destroyed? Should use Lifecycle undo for that.
            }

            if (locRes.Value.archetypeID != archId)
            {
                ref var targetArchetype = ref world.ComponentManager.GetArchetypeReference(archId);

                // Build ComponentSetView from the target archetype
                var it = targetArchetype._signature.GetIterator();
                var components = new List<Identifier<IComponent>>();
                while (it.Next(out var compId))
                {
                    components.Add(new Identifier<IComponent>(compId));
                }

                var set = new ComponentSetView(components.ToArray(), sharedData ?? Array.Empty<byte>());
                world.EntityManager.MigrateEntity(targetEntity, set);
            }


            // Overwrite unmanaged memory
            locRes = world.EntityManager.GetEntityLocation(targetEntity);
            if (locRes.IsSuccess)
            {
                unsafe
                {
                    ref var archetype = ref world.ComponentManager.GetArchetypeReference(locRes.Value.archetypeID);
                    ref var chunk = ref archetype.GetChunkReference(locRes.Value.chunkIndex);

                    fixed (byte* pSrcBase = compData)
                    {
                        var offset = 0;
                        for (var i = 0; i < archetype._layouts.Count; i++)
                        {
                            var layout = archetype._layouts[i];
                            var pDst = chunk.GetUnsafePtr() + layout.offset + (layout.size * locRes.Value.rowIndex);
                            Buffer.MemoryCopy(pSrcBase + offset, pDst, layout.size, layout.size);
                            offset += layout.size;
                        }
                    }
                }
            }
        });
    }

    public override bool CanMerge(UndoOperation other)
    {
        if (other is EntityStructureOperation op)
        {
            return op.Entity == Entity && op.GroupId == GroupId;
        }
        return false;
    }
}

public class EntityLifecycleOperation : UndoOperation
{
    public Entity Entity { get; set; }
    public Guid InstanceID { get; set; }
    public LifecycleEvent EventType { get; set; }

    // State for destruction
    public int ArchetypeID { get; set; }
    public byte[] ComponentData { get; set; } = Array.Empty<byte>();
    public byte[] SharedData { get; set; } = Array.Empty<byte>();
    public int SharedDataHash { get; set; }

    public override UndoOperation CreateReciprocal(IEditorWorldService worldService)
    {
        var reciprocal = new EntityLifecycleOperation
        {
            GroupId = GroupId,
            ActionName = ActionName,
            Entity = Entity,
            InstanceID = InstanceID,
            EventType = EventType == LifecycleEvent.Created ? LifecycleEvent.Destroyed : LifecycleEvent.Created,
            ArchetypeID = ArchetypeID,
            ComponentData = ComponentData,
            SharedData = SharedData,
            SharedDataHash = SharedDataHash
        };
        return reciprocal;
    }

    public override void Revert(IEditorWorldService worldService)
    {
        worldService.Defer(() =>
        {
            if (EventType == LifecycleEvent.Created)
            {
                // Revert a Creation = Destroy
                var node = GhostObject.Find(InstanceID) as EntityNode;
                var targetEntity = node?.Entity ?? Entity;
                worldService.EditorWorld.EntityManager.DestroyEntity(targetEntity);
                // The InstanceID GhostObject will be naturally unlinked, handles become null
            }
            else
            {
                // Revert a Destruction = Recreate
                var newEntity = worldService.EditorWorld.EntityManager.CreateEntity();

                // TODO: Apply the ArchetypeID, ComponentData, SharedData to the newEntity.
                // We'd add the components using the archetype signature, then memcopy the bytes.

                // Fix the Node reference
                if (GhostObject.Find(InstanceID) is not EntityNode node)
                {
                    node = new EntityNode(worldService.EditorWorld, newEntity, "Resurrected");
                    // Force the InstanceID using backing field
                    var backingField = typeof(EntityNode).GetField("<InstanceID>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                       ?? typeof(SceneGraphNode).GetField("<InstanceID>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    backingField?.SetValue(node, InstanceID);
                }
                else
                {
                    // Update the entity property of the existing node (using reflection since it's init/readonly)
                    var entityField = typeof(EntityNode).GetField("<Entity>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    entityField?.SetValue(node, newEntity);
                }
            }
        });
    }
}

public class UndoService : IUndoService
{
    public event Action? UndoRedoPerformed;

    private readonly IEditorWorldService _worldService;
    private readonly RingBuffer<UndoOperation> _undoStack = new(50);
    private readonly Stack<UndoOperation> _redoStack = new();

    private int _nextGroupId = 1;
    private int _activeGroupId = 0;

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
    }

    public void RecordObject(GhostObject obj, string actionName)
    {
        var op = new ObjectStateOperation
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

    public void RecordEntityComponent(ComponentNode node, string actionName)
    {
        var op = new EntityComponentOperation
        {
            ActionName = actionName,
            Entity = node.Entity,
            ComponentId = node.Descriptor.ComponentId,
            InstanceID = node.EntityNode.InstanceID
        };

        unsafe
        {
            var pComp = node.GetComponentPointer();
            var size = node.Descriptor.Size;
            var data = new byte[size];
            fixed (byte* pDst = data)
            {
                Buffer.MemoryCopy(pComp, pDst, size, size);
            }
            op.ComponentData = data;
        }

        PushOperation(op);
    }

    public void RecordEntityStructure(EntityNode node, string actionName)
    {
        var op = EntityStructureOperation.Capture(_worldService, node);
        op.ActionName = actionName;
        PushOperation(op);
    }

    public void RecordEntityLifecycle(EntityNode node, LifecycleEvent type)
    {
        var op = new EntityLifecycleOperation
        {
            ActionName = type == LifecycleEvent.Created ? "Create Entity" : "Destroy Entity",
            Entity = node.Entity,
            InstanceID = node.InstanceID,
            EventType = type
        };

        if (type == LifecycleEvent.Destroyed)
        {
            // Capture state before destruction
            var structure = EntityStructureOperation.Capture(_worldService, node);
            op.ArchetypeID = structure.ArchetypeID;
            op.ComponentData = structure.ComponentData;
            op.SharedData = structure.SharedData;
            op.SharedDataHash = structure.SharedDataHash;
        }

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

        _worldService.FlushCommands();

        UndoRedoPerformed?.Invoke();
    }
}
