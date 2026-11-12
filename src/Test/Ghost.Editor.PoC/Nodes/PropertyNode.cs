using Ghost.Editor.PoC.Sync;
using System.Reflection;

namespace Ghost.Editor.PoC.Nodes;

public class PropertyNode
{
    public string Name => Field.Name;
    public Type PropertyType => Field.FieldType;
    public FieldInfo Field { get; }
    public ComponentNode ComponentNode { get; }
    public EditorSyncBridge Bridge { get; }

    public PropertyNode(ComponentNode componentNode, FieldInfo field, EditorSyncBridge bridge)
    {
        ComponentNode = componentNode;
        Field = field;
        Bridge = bridge;
    }

    public object? GetValue()
    {
        var entity = ComponentNode.EntityNode.EcsEntity;
        if (Bridge.TryGetSnapshotValue(entity, ComponentNode.ComponentType, Field.Name, out var value))
        {
            return value;
        }

        return null;
    }

    public void SetValue(object? value)
    {
        var entity = ComponentNode.EntityNode.EcsEntity;
        Bridge.EnqueueMutation(entity, ComponentNode.ComponentType, Field.Name, value);
    }
}
