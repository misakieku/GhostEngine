using Ghost.AssetForge.Core.Models;
using Ghost.Entities;
using System.Reflection;
using System.Text.Json;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Editor-only authoring link tying a scene <see cref="EntityNode"/> back to its source prefab instance and template entity index.
/// Never attached as a runtime ECS component.
/// </summary>
public sealed record PrefabLinkInfo(Guid InstanceGuid, Guid PrefabAssetGuid, int TemplateEntityIndex);

/// <summary>
/// Shared base type for nodes displayed in the editor Scene Hierarchy tree.
/// </summary>
public abstract class HierarchyNode
{
    public int Id { get; }
    public string Name { get; set; }
    public int? ParentId { get; set; }

    public virtual bool CanRename => false;
    public virtual bool CanReparent => false;
    public virtual bool CanDelete => false;

    protected HierarchyNode(int id, string name, int? parentId = null)
    {
        Id = id;
        Name = name;
        ParentId = parentId;
    }
}

/// <summary>
/// Represents a loaded editor scene root entry in the Scene Hierarchy and Inspector.
/// </summary>
public sealed class SceneNode : HierarchyNode
{
    public ushort SceneId { get; }
    public string? ScenePath { get; set; }
    public bool IsDirty { get; set; }
    public int SceneEntityCount { get; set; }

    public SceneNode(int id, string name, ushort sceneId, string? scenePath = null, bool isDirty = false, int entityCount = 0)
        : base(id, name, null)
    {
        SceneId = sceneId;
        ScenePath = scenePath;
        IsDirty = isDirty;
        SceneEntityCount = entityCount;
    }
}

public sealed class EntityNode : HierarchyNode
{
    public Guid EditorGuid { get; set; } = Guid.NewGuid();
    public ushort SceneId { get; set; }
    public Entity EcsEntity { get; set; } = Entity.Invalid;
    public PrefabLinkInfo? PrefabLink { get; set; }
    public IReadOnlyList<ComponentNode> Components { get; set; }

    public override bool CanRename => true;
    public override bool CanReparent => true;
    public override bool CanDelete => true;

    public EntityNode(int id, string name, IReadOnlyList<ComponentNode> components, int? parentId = null)
        : base(id, name, parentId)
    {
        Components = components;
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

    public PropertyNode? FindProperty(string propertyName)
    {
        for (var i = 0; i < Properties.Count; i++)
        {
            if (string.Equals(Properties[i].Name, propertyName, StringComparison.Ordinal))
            {
                return Properties[i];
            }
        }

        return null;
    }

    public SceneComponentRecord SerializeToRecord(JsonSerializerOptions options)
    {
        var record = new SceneComponentRecord
        {
            Type = ComponentType.FullName ?? ComponentType.Name
        };

        for (var i = 0; i < Properties.Count; i++)
        {
            Properties[i].WriteToComponentRecord(record, options);
        }

        return record;
    }

    public void DeserializeFromRecord(SceneComponentRecord record, JsonSerializerOptions options)
    {
        for (var i = 0; i < Properties.Count; i++)
        {
            Properties[i].ReadFromComponentRecord(record, options);
        }
    }
}

public abstract class PropertyNode
{
    public string Name { get; }
    public string DisplayName { get; }
    public Type PropertyType { get; }
    public ComponentNode ComponentNode { get; set; }
    public object? Value { get; set; }
    public bool IsReadOnly { get; }
    public string? GroupName { get; }
    public Action<object?> OnChange { get; }

    protected PropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, bool isReadOnly = false, string? displayName = null, string? groupName = null)
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

    /// <summary>
    /// Applies an authoring or command value update to this node and propagates the runtime mutation to ECS.
    /// </summary>
    public abstract void ApplyValue(object? newValue);

    /// <summary>
    /// Refreshes this node from live ECS component data without clobbering authoring-only metadata (such as asset GUIDs).
    /// </summary>
    public virtual void RefreshFromEcs(object boxedComponent, FieldInfo? field)
    {
        if (field != null)
        {
            Value = field.GetValue(boxedComponent);
        }
    }

    /// <summary>
    /// Returns the authoring value to persist during scene serialization (never ephemeral runtime handles).
    /// </summary>
    public abstract object? GetSerializableValue();

    /// <summary>
    /// Serializes this node's authoring value into a <see cref="JsonElement"/> without exposing runtime handles.
    /// </summary>
    public virtual JsonElement SerializeToJson(JsonSerializerOptions options)
    {
        var value = GetSerializableValue();
        return value == null ? default : JsonSerializer.SerializeToElement(value, PropertyType, options);
    }

    /// <summary>
    /// Deserializes a <see cref="JsonElement"/> payload and applies it to this property node.
    /// </summary>
    public virtual void DeserializeFromJson(JsonElement element, JsonSerializerOptions options)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return;
        }

        var value = element.Deserialize(PropertyType, options);
        ApplyValue(value);
    }

    /// <summary>
    /// Writes this property node's serialized state into a <see cref="SceneComponentRecord"/>.
    /// </summary>
    public virtual void WriteToComponentRecord(SceneComponentRecord record, JsonSerializerOptions options)
    {
        var elem = SerializeToJson(options);
        if (elem.ValueKind != JsonValueKind.Undefined)
        {
            record.Fields[Name] = elem;
        }
    }

    /// <summary>
    /// Reads this property node's serialized state from a <see cref="SceneComponentRecord"/>.
    /// </summary>
    public virtual void ReadFromComponentRecord(SceneComponentRecord record, JsonSerializerOptions options)
    {
        if (record.Fields.TryGetValue(Name, out var elem))
        {
            DeserializeFromJson(elem, options);
        }
    }

    /// <summary>
    /// Gets whether this property must be re-bound after all entities in a loaded scene snapshot are registered.
    /// </summary>
    public virtual bool RequiresPostSnapshotBind => false;

    /// <summary>
    /// Completes any deferred async asset resolution once underlying streaming entries become ready.
    /// </summary>
    public virtual bool TryCompletePendingResolution()
    {
        return false;
    }

    /// <summary>
    /// Releases any runtime or streaming asset references held by this property node.
    /// </summary>
    public virtual void ReleaseResources()
    {
    }

    /// <summary>
    /// Creates a deep copy of this property node bound to <paramref name="targetComponent"/>.
    /// </summary>
    public abstract PropertyNode Clone(ComponentNode targetComponent);
}
