using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SceneAssetDocument))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float[]))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(uint[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Guid[]))]
public partial class SceneAssetDocumentContext : JsonSerializerContext
{
}

/// <summary>
/// Pure-data authoring representation of a <c>.gscene</c> scene file on disk.
/// Uses persistent <see cref="Guid"/> identities and stores placed prefabs by reference with sparse overrides.
/// </summary>
public sealed class SceneAssetDocument
{
    public const string EXTENSION = ".gscene";
    public const int CURRENT_VERSION = 1;

    public int Version { get; set; } = CURRENT_VERSION;

    public List<SceneEntityRecord> Entities { get; set; } = new();

    public List<ScenePrefabInstanceRecord> PrefabInstances { get; set; } = new();
}

/// <summary>
/// Serialized authoring state for a standalone entity in a scene.
/// Array order preserves hierarchy sibling ordering.
/// </summary>
public sealed class SceneEntityRecord
{
    public Guid Guid { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? ParentGuid { get; set; }

    public List<SceneComponentRecord> Components { get; set; } = new();
}

/// <summary>
/// Serialized authoring state for a single ECS component attached to an entity.
/// </summary>
public sealed class SceneComponentRecord
{
    public string Type { get; set; } = string.Empty;

    public Dictionary<string, JsonElement> Fields { get; set; } = new(StringComparer.Ordinal);

    public List<SceneVolumeOverrideRecord>? VolumeOverrides { get; set; }
}

/// <summary>
/// Serialized authoring state for one overridden volume setting inside <c>RenderVolumeDataContainer</c>.
/// </summary>
public sealed class SceneVolumeOverrideRecord
{
    public string Type { get; set; } = string.Empty;

    public Dictionary<string, JsonElement> Fields { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Reference to a placed prefab asset within an authoring scene alongside per-instance overrides.
/// Flattened into plain entities only during runtime materialization so prefab edits do not trigger scene rebakes.
/// </summary>
public sealed class ScenePrefabInstanceRecord
{
    public Guid InstanceGuid { get; set; }

    public Guid PrefabGuid { get; set; }

    public Guid? ParentGuid { get; set; }

    /// <summary>
    /// Maps each prefab template entity index to a stable scene <see cref="Guid"/> so scene entities
    /// and entity references can target specific child nodes inside the prefab instance.
    /// </summary>
    public Dictionary<int, Guid> NodeGuids { get; set; } = new();

    public Dictionary<int, string>? NameOverrides { get; set; }

    public List<ScenePropertyOverrideRecord>? PropertyOverrides { get; set; }

    public List<SceneAddedComponentRecord>? AddedComponents { get; set; }

    public List<SceneRemovedComponentRecord>? RemovedComponents { get; set; }
}

/// <summary>
/// Sparse field-level override targeting a component on a specific node inside a prefab instance.
/// </summary>
public sealed class ScenePropertyOverrideRecord
{
    public Guid TargetGuid { get; set; }

    public string Component { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public JsonElement Value { get; set; }
}

/// <summary>
/// Additional component attached in the scene to a node belonging to a prefab instance.
/// </summary>
public sealed class SceneAddedComponentRecord
{
    public Guid TargetGuid { get; set; }

    public string Component { get; set; } = string.Empty;

    public Dictionary<string, JsonElement> Fields { get; set; } = new(StringComparer.Ordinal);

    public List<SceneVolumeOverrideRecord>? VolumeOverrides { get; set; }
}

/// <summary>
/// Records removal of a default prefab component from a specific node in a prefab instance.
/// </summary>
public sealed class SceneRemovedComponentRecord
{
    public Guid TargetGuid { get; set; }

    public string Component { get; set; } = string.Empty;
}
