using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.Serialization;

/// <summary>
/// Represents the serialized data for a scene in JSON format.
/// This is editor-only and used for scene file persistence.
/// </summary>
public class SceneData
{
    /// <summary>
    /// The name of the scene.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "New Scene";

    /// <summary>
    /// List of entities in this scene, ordered by file-local ID.
    /// The index in this list IS the file-local ID.
    /// </summary>
    [JsonPropertyName("entities")]
    public List<EntityData> Entities { get; set; } = [];
}

/// <summary>
/// Represents the serialized data for an entity.
/// </summary>
public class EntityData
{
    /// <summary>
    /// The display name of this entity (editor-only).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Entity";

    /// <summary>
    /// The file-local ID of the parent entity, or -1 if root.
    /// </summary>
    [JsonPropertyName("parent")]
    public int ParentLocalId { get; set; } = -1;

    /// <summary>
    /// Dictionary of component data, keyed by component type name.
    /// </summary>
    [JsonPropertyName("components")]
    public Dictionary<string, ComponentData> Components { get; set; } = [];
}

/// <summary>
/// Represents the serialized data for a component.
/// </summary>
public class ComponentData
{
    /// <summary>
    /// The component type full name (for deserialization).
    /// </summary>
    [JsonPropertyName("type")]
    public string TypeName { get; set; } = "";

    /// <summary>
    /// The serialized component fields.
    /// Entity references are stored as file-local IDs (integers).
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, object?> Fields { get; set; } = [];
}
