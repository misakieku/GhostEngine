using Ghost.Entities;

namespace Ghost.Engine.Core;

/// <summary>
/// Represents a lightweight handle to a loaded scene.
/// </summary>
/// <remarks>
/// A Scene is a collection of entities tagged with a unique SceneID component.
/// The Scene class provides a convenient handle to interact with all entities
/// belonging to a particular scene within a World.
/// </remarks>
public sealed class Scene : IDisposable, IEquatable<Scene>
{
    private static short s_nextSceneID = 0;

    private readonly World _world;
    private readonly short _id;
    private readonly string _name;
    private bool _isDisposed;

    /// <summary>
    /// Gets the world this scene belongs to.
    /// </summary>
    public World World => _world;

    /// <summary>
    /// Gets the unique identifier for this scene.
    /// </summary>
    public short ID => _id;

    /// <summary>
    /// Gets the name of this scene.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Creates a new scene handle.
    /// </summary>
    /// <param name="world">The world this scene belongs to.</param>
    /// <param name="name">The name of the scene.</param>
    internal Scene(World world, string name)
    {
        _world = world;
        _id = s_nextSceneID++;
        _name = name;
    }

    /// <summary>
    /// Creates a new scene handle with a specific ID.
    /// </summary>
    /// <param name="world">The world this scene belongs to.</param>
    /// <param name="id">The scene ID.</param>
    /// <param name="name">The name of the scene.</param>
    internal Scene(World world, short id, string name)
    {
        _world = world;
        _id = id;
        _name = name;

        // Update next ID if necessary
        if (id >= s_nextSceneID)
        {
            s_nextSceneID = (short)(id + 1);
        }
    }

    ~Scene()
    {
        Dispose();
    }

    public bool Equals(Scene? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _world.Equals(other._world) && _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Scene other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_world, _id);
    }

    public override string ToString()
    {
        return $"Scene: {_name} (ID: {_id})";
    }

    public static bool operator ==(Scene? left, Scene? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public static bool operator !=(Scene? left, Scene? right)
    {
        return !(left == right);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
