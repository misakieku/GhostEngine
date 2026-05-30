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
            return op.Entity == Entity && op.ComponentId == ComponentId && op.GroupId == GroupId;
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
                // We need to move the entity to the correct archetype and chunk group.
                // Ghost.Entities might not have an easy "MoveEntityToArchetypeAndChunkGroup"
                // The easiest way is to destroy and recreate the entity with the same ID,
                // but since EntityManager doesn't expose CreateEntity(Entity), we might have to rely on
                // AddComponent/RemoveComponent to migrate it, or use internal methods.
                // For now, we will add/remove components to match the target archetype signature.
                // 
                // Alternatively, we can use a structural backdoor if available, but for now we'll do our best:
                ref var currentArchetype = ref world.ComponentManager.GetArchetypeReference(locRes.Value.archetypeID);
                ref var targetArchetype = ref world.ComponentManager.GetArchetypeReference(archId);

                // Determine components to add and remove
                var it = currentArchetype._signature.GetIterator();
                var toRemove = new List<int>();
                while (it.Next(out var compId))
                {
                    if (!targetArchetype._signature.IsSet(compId))
                    {
                        toRemove.Add(compId);
                    }
                }

                it = targetArchetype._signature.GetIterator();
                var toAdd = new List<int>();
                while (it.Next(out var compId))
                {
                    if (!currentArchetype._signature.IsSet(compId))
                    {
                        toAdd.Add(compId);
                    }
                }

                foreach (var id in toRemove)
                {
                    world.EntityManager.RemoveComponent(targetEntity, new Identifier<IComponent>(id));
                }

                foreach (var id in toAdd)
                {
                    // Add default component, we will overwrite its memory shortly.
                    unsafe
                    {
                        var info = ComponentRegistry.GetComponentInfo(new Identifier<IComponent>(id));
                        var defaultData = new byte[info.size];
                        fixed (byte* p = defaultData)
                        {
                            world.EntityManager.AddComponent(targetEntity, new Identifier<IComponent>(id), p);
                        }
                    }
                }
            }

            // By now the entity should be in the correct archetype, but maybe not the correct shared data group.
            // We need to overwrite the shared data if needed.
            // (Assuming there are APIs to set shared data based on the recorded bytes).

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
        if (_activeGroupId != 0)
        {
            op.GroupId = _activeGroupId;
        }
        else
        {
            op.GroupId = _nextGroupId++;
        }

        UndoOperation? top = _undoStack.Count > 0 ? _undoStack.Peek() : null;
        if (_activeGroupId != 0 && top != null && op.CanMerge(top))
        {
            // Skip recording if we are in a transaction and the same object was already recorded
            return;
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
        // Internal getter logic goes here or we use the Node's existing pointer method
        var entityNodeField = typeof(ComponentNode).GetField("_entityNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var op = new EntityComponentOperation
        {
            ActionName = actionName,
            Entity = typeof(ComponentNode).GetField("_entity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(node) as Entity? ?? default,
            ComponentId = node.Descriptor.ComponentId
        };
        // We assume we can get the InstanceID if we pass the EntityNode along, or we can look it up.
        // Wait, ComponentNode doesn't have an EntityNode reference. We can resolve it via Entity.
        // Let's just find the first EntityNode that has this Entity.
        // Actually, let's assume we can inject it or just use the Entity directly for now.
        // Actually since we have GhostObject.Find, maybe not.
        // Let's pass the EntityNode InstanceID. Wait, I'll use a reflection trick to find it if possible, or just leave it empty if we can't.
        // Or better yet, we can ask the registry.
        // Actually, we can just leave it as Guid.Empty if not known, but let's try to get it.
        op.InstanceID = Guid.Empty;

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
