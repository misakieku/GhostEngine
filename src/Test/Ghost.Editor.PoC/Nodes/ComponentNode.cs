using Ghost.Editor.PoC.Sync;
using System.Reflection;

namespace Ghost.Editor.PoC.Nodes;

public class ComponentNode
{
    public Type ComponentType { get; }
    public string DisplayName { get; }
    public EntityNode EntityNode { get; }
    public List<PropertyNode> Properties { get; } = new();

    public ComponentNode(EntityNode entityNode, Type componentType, EditorSyncBridge bridge)
    {
        EntityNode = entityNode;
        ComponentType = componentType;
        DisplayName = componentType.Name;

        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        for (var i = 0; i < fields.Length; i++)
        {
            Properties.Add(new PropertyNode(this, fields[i], bridge));
        }
    }
}
