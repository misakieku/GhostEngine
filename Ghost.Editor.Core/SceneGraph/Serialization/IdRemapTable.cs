using System.Collections.ObjectModel;
using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph.Serialization;

/// <summary>
/// Maps file-local entity IDs to global entity IDs.
/// Used when loading scenes to remap entity references from file-local IDs to runtime global IDs.
/// </summary>
public class IdRemapTable
{
    /// <summary>
    /// Maps file-local ID (index) to global Entity ID.
    /// </summary>
    private readonly Dictionary<int, Entity> _localToGlobal;

    /// <summary>
    /// Maps global Entity ID to file-local ID.
    /// </summary>
    private readonly Dictionary<Entity, int> _globalToLocal;

    public IdRemapTable()
    {
        _localToGlobal = new Dictionary<int, Entity>();
        _globalToLocal = new Dictionary<Entity, int>();
    }

    /// <summary>
    /// Registers the mapping between a file-local ID and global entity ID.
    /// </summary>
    public void Register(int fileLocalId, Entity globalEntityId)
    {
        if (fileLocalId < 0)
            throw new ArgumentException("File-local ID must be >= 0", nameof(fileLocalId));

        _localToGlobal[fileLocalId] = globalEntityId;
        _globalToLocal[globalEntityId] = fileLocalId;
    }

    /// <summary>
    /// Gets the global entity ID for a file-local ID.
    /// Returns Entity.Invalid if not found.
    /// </summary>
    public Entity GetGlobalId(int fileLocalId)
    {
        _localToGlobal.TryGetValue(fileLocalId, out var globalId);
        return globalId;
    }

    /// <summary>
    /// Gets the file-local ID for a global entity ID.
    /// Returns -1 if not found.
    /// </summary>
    public int GetLocalId(Entity globalEntityId)
    {
        return _globalToLocal.TryGetValue(globalEntityId, out var localId) ? localId : -1;
    }

    /// <summary>
    /// Remaps an entity reference from file-local ID to global ID.
    /// Throws if the file-local ID is not registered.
    /// </summary>
    public Entity RemapReference(int fileLocalId)
    {
        if (!_localToGlobal.TryGetValue(fileLocalId, out var globalId))
        {
            throw new KeyNotFoundException($"File-local entity ID {fileLocalId} not found in remap table.");
        }

        return globalId;
    }

    /// <summary>
    /// Gets the count of mapped entities.
    /// </summary>
    public int Count => _localToGlobal.Count;

    /// <summary>
    /// Gets all mapped local->global pairs.
    /// </summary>
    public IEnumerable<KeyValuePair<int, Entity>> GetMappings()
    {
        return _localToGlobal.AsEnumerable();
    }
}

/// <summary>
/// Contains context information for loading or saving a scene.
/// Includes component type information and entity remapping logic.
/// </summary>
public class SceneSerializationContext
{
    /// <summary>
    /// Maps file-local entity IDs to runtime global entity IDs.
    /// </summary>
    public IdRemapTable IdRemap { get; }

    /// <summary>
    /// Scene ID being serialized/deserialized.
    /// </summary>
    public short SceneId { get; }

    /// <summary>
    /// Editor world where entities are being loaded/saved.
    /// </summary>
    public World EditorWorld { get; }

    /// <summary>
    /// List of entities in the order they appear in the saved file.
    /// Index corresponds to file-local ID.
    /// </summary>
    public List<Entity> EntityOrder { get; }

    /// <summary>
    /// Validation errors encountered during serialization.
    /// </summary>
    public List<string> ValidationErrors { get; }

    public SceneSerializationContext(short sceneId, World editorWorld)
    {
        SceneId = sceneId;
        EditorWorld = editorWorld ?? throw new ArgumentNullException(nameof(editorWorld));
        IdRemap = new IdRemapTable();
        EntityOrder = new List<Entity>();
        ValidationErrors = new List<string>();
    }

    /// <summary>
    /// Adds a validation error message.
    /// </summary>
    public void AddValidationError(string message)
    {
        ValidationErrors.Add(message);
    }

    /// <summary>
    /// Returns true if there are any validation errors.
    /// </summary>
    public bool HasErrors => ValidationErrors.Count > 0;

    /// <summary>
    /// Gets all validation errors as a single string.
    /// </summary>
    public string GetErrorsSummary()
    {
        return string.Join("\n", ValidationErrors);
    }
}
