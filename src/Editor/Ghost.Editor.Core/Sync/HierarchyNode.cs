using Ghost.Entities;

namespace Ghost.Editor.Core.Sync;

public sealed class EntityNode
{
    public int Id { get; }
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public Entity EcsEntity { get; set; } = Entity.Invalid;
    public IReadOnlyList<ComponentNode> Components { get; set; }

    public EntityNode(int id, string name, IReadOnlyList<ComponentNode> components, int? parentId = null)
    {
        Id = id;
        Name = name;
        Components = components;
        ParentId = parentId;
    }
}

public sealed class ComponentNode
{
    public string TypeName { get; }
    public Type ComponentType { get; }
    public EntityNode EntityNode { get; set; }
    public IReadOnlyList<PropertyNode> Properties { get; set; }
    public object? LiveValue { get; set; }

    public ComponentNode(string typeName, Type componentType, EntityNode entityNode, IReadOnlyList<PropertyNode> properties, object? liveValue = null)
    {
        TypeName = typeName;
        ComponentType = componentType;
        EntityNode = entityNode;
        Properties = properties;
        LiveValue = liveValue;
    }
}

public sealed class PropertyNode
{
    public string Name { get; }
    public string DisplayName { get; }
    public Type PropertyType { get; }
    public ComponentNode ComponentNode { get; set; }
    public object? Value { get; set; }
    public bool IsReadOnly { get; }
    public string? GroupName { get; }
    public Action<object?> OnChange { get; }

    public PropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, bool isReadOnly = false, string? displayName = null, string? groupName = null)
    {
        Name = name;
        DisplayName = displayName ?? name;
        PropertyType = propertyType;
        ComponentNode = componentNode;
        Value = value;
        IsReadOnly = isReadOnly;
        GroupName = groupName;
        OnChange = onChange;
    }
}
