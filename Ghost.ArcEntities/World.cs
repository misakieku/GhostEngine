using System.Runtime.CompilerServices;

namespace Ghost.ArcEntities;

public partial class World
{
    private static List<World> s_worlds = new(4);
    private static Queue<WorldID> s_freeWorldSlots = new();

    public static int WorldCount => s_worlds.Count - s_freeWorldSlots.Count;

    public static World Create(int entityCapacity = 16)
    {
        lock (s_worlds)
        {
            if (s_freeWorldSlots.TryDequeue(out var index))
            {
                s_worlds[index] = new World(index, entityCapacity);
            }
            else
            {
                if (s_worlds.Count >= WorldID.MaxValue)
                {
                    throw new InvalidOperationException("Maximum number of worlds reached");
                }

                index = (WorldID)s_worlds.Count;
                s_worlds.Add(new World(index, entityCapacity));
            }

            return s_worlds[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static World GetWorld(int index)
    {
        return s_worlds[index];
    }
}

public partial class World : IDisposable, IEquatable<World>
{
    private readonly WorldID _id;

    private bool _isDisposed = false;

    public WorldID ID => _id;

    private World(WorldID id, int entityCapacity)
    {
        _id = id;
    }

    ~World()
    {
        Dispose();
    }

    public bool Equals(World? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _id == other._id;
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is World other && Equals(other);
    }

    public static bool operator ==(World? left, World? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(World? left, World? right)
    {
        return !(left == right);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        s_freeWorldSlots.Enqueue(_id);

        _isDisposed = true;

        GC.SuppressFinalize(this);
    }
}

