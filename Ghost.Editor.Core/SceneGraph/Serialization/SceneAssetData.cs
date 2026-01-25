using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.SceneGraph.Serialization;

/// <summary>
/// JSON-serializable representation of a component instance.
/// Only used in the editor for saving/loading scenes.
/// </summary>
[Serializable]
public class ComponentData
{
    /// <summary>
    /// Fully qualified type name of the component (e.g., "Ghost.Engine.Components.Transform").
    /// </summary>
    [JsonPropertyName("type")]
    public string ComponentTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Serialized component data as a dictionary.
    /// Field names map to JSON values.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object?> Data { get; set; } = new();
}

/// <summary>
/// JSON-serializable representation of an entity within a scene.
/// Only used in the editor for saving/loading scenes.
/// 
/// The index in the entities list corresponds to the file-local ID.
/// </summary>
[Serializable]
public class EntityData
{
    /// <summary>
    /// File-local entity ID within the scene.
    /// Set by the serializer based on position in the entities list.
    /// </summary>
    [JsonPropertyName("fileLocalId")]
    public int FileLocalId { get; set; }

    /// <summary>
    /// Editor-only name for the entity.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Entity";

    /// <summary>
    /// File-local ID of the parent entity, or -1 if root.
    /// </summary>
    [JsonPropertyName("parentFileLocalId")]
    public int ParentFileLocalId { get; set; } = -1;

    /// <summary>
    /// All components attached to this entity.
    /// </summary>
    [JsonPropertyName("components")]
    public List<ComponentData> Components { get; set; } = new();
}

/// <summary>
/// JSON-serializable representation of a scene.
/// Only used in the editor for saving/loading scenes.
/// </summary>
[Serializable]
public class SceneAssetData
{
    /// <summary>
    /// Scene metadata version for forward compatibility.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Unique identifier for this scene (GUID).
    /// </summary>
    [JsonPropertyName("sceneGuid")]
    public Guid SceneGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Editor-friendly name of the scene.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Scene";

    /// <summary>
    /// Runtime scene ID.
    /// </summary>
    [JsonPropertyName("sceneId")]
    public short SceneId { get; set; }

    /// <summary>
    /// All entities in the scene, ordered by file-local ID.
    /// Index in this list == file-local ID.
    /// </summary>
    [JsonPropertyName("entities")]
    public List<EntityData> Entities { get; set; } = new();
}
