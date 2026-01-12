namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Blackboard for sharing data between render passes.
/// Uses a dictionary with type keys to store different pass data types.
/// Avoids allocations by reusing the same dictionary across frames.
/// </summary>
public sealed class RenderGraphBlackboard
{
    private readonly Dictionary<Type, IPassData> _data = new(16);

    /// <summary>
    /// Adds or updates pass data in the blackboard.
    /// </summary>
    public void Add<T>(T data)
        where T : class, IPassData
    {
        var type = typeof(T);
        _data[type] = data;
    }

    /// <summary>
    /// Retrieves pass data from the blackboard.
    /// </summary>
    public T Get<T>()
        where T : class, IPassData
    {
        var type = typeof(T);
        if (_data.TryGetValue(type, out var obj))
        {
            return (T)obj;
        }

        throw new KeyNotFoundException($"Pass data of type {type.Name} not found in blackboard");
    }

    /// <summary>
    /// Tries to get pass data from the blackboard.
    /// </summary>
    public bool TryGet<T>(out T? data)
        where T : class, IPassData
    {
        var type = typeof(T);
        if (_data.TryGetValue(type, out var obj))
        {
            data = (T)obj;
            return true;
        }

        data = null;
        return false;
    }

    /// <summary>
    /// Clears all data from the blackboard.
    /// Does not deallocate the backing dictionary to avoid allocations.
    /// </summary>
    public void Clear()
    {
        _data.Clear();
    }
}
