using Ghost.Editor.PoC.Sync;
using Ghost.Entities;

namespace Ghost.Editor.PoC.Nodes;

public class EntityNode
{
    public Guid EditorId { get; }
    public string Name { get; set; }
    public Entity EcsEntity { get; set; }
    public EditorSyncBridge Bridge { get; }
    public List<ComponentNode> Components { get; } = new();

    public EntityNode(Guid editorId, string name, Entity ecsEntity, EditorSyncBridge bridge)
    {
        EditorId = editorId;
        Name = name;
        EcsEntity = ecsEntity;
        Bridge = bridge;
    }

    public ComponentNode AddComponent(Type componentType)
    {
        var node = new ComponentNode(this, componentType, Bridge);
        Components.Add(node);
        return node;
    }
}
