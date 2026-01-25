using Ghost.Entities;

namespace Ghost.Engine.IO;

/// <summary>
/// Provides a thread-safe context for Entity ID remapping during deserialization.
/// </summary>
/// <remarks>
/// This class manages the mapping between file-local entity IDs (0, 1, 2...) 
/// and runtime entity IDs during scene deserialization. The context is scoped 
/// to the current async operation using AsyncLocal storage.
/// </remarks>
public sealed class SerializationContext : IDisposable
{
    private static readonly AsyncLocal<SerializationContext?> s_current = new();

    private readonly Dictionary<int, Entity> _fileIdToEntity = new();
    private readonly Dictionary<Entity, int> _entityToFileId = new();
    private int _nextFileId = 0;
    private bool _disposed = false;

    /// <summary>
    /// Gets the current serialization context for this async operation.
    /// </summary>
    public static SerializationContext? Current => s_current.Value;

    private SerializationContext()
    {
    }

    /// <summary>
    /// Creates and activates a new serialization context for the current async scope.
    /// </summary>
    /// <returns>A new serialization context. Must be disposed when done.</returns>
    public static SerializationContext Create()
    {
        if (s_current.Value != null)
        {
            throw new InvalidOperationException("A serialization context is already active in this scope.");
        }

        var context = new SerializationContext();
        s_current.Value = context;
        return context;
    }

    /// <summary>
    /// Registers an entity mapping for deserialization.
    /// </summary>
    /// <param name="fileId">The file-local entity ID.</param>
    /// <param name="runtimeEntity">The runtime entity.</param>
    public void RegisterEntity(int fileId, Entity runtimeEntity)
    {
        _fileIdToEntity[fileId] = runtimeEntity;
        _entityToFileId[runtimeEntity] = fileId;
    }

    /// <summary>
    /// Registers a runtime entity and assigns it the next available file ID for serialization.
    /// </summary>
    /// <param name="runtimeEntity">The runtime entity to register.</param>
    /// <returns>The assigned file-local ID.</returns>
    public int RegisterEntityForSerialization(Entity runtimeEntity)
    {
        if (!_entityToFileId.TryGetValue(runtimeEntity, out var fileId))
        {
            fileId = _nextFileId++;
            _entityToFileId[runtimeEntity] = fileId;
            _fileIdToEntity[fileId] = runtimeEntity;
        }

        return fileId;
    }

    /// <summary>
    /// Tries to get the runtime entity for a file-local ID.
    /// </summary>
    /// <param name="fileId">The file-local entity ID.</param>
    /// <param name="entity">The runtime entity if found.</param>
    /// <returns>True if the entity was found, false otherwise.</returns>
    public bool TryGetEntity(int fileId, out Entity entity)
    {
        return _fileIdToEntity.TryGetValue(fileId, out entity);
    }

    /// <summary>
    /// Tries to get the file-local ID for a runtime entity.
    /// </summary>
    /// <param name="entity">The runtime entity.</param>
    /// <param name="fileId">The file-local ID if found.</param>
    /// <returns>True if the file ID was found, false otherwise.</returns>
    public bool TryGetFileId(Entity entity, out int fileId)
    {
        return _entityToFileId.TryGetValue(entity, out fileId);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        s_current.Value = null;
        _fileIdToEntity.Clear();
        _entityToFileId.Clear();
        _disposed = true;
    }
}
