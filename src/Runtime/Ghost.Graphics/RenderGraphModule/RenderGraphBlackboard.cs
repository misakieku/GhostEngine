using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Blackboard for sharing data between render passes.
/// </summary>
public sealed class RenderGraphBlackboard
{
    private readonly Dictionary<Type, RenderGraphPass> _data = new(16);

    /// <summary>
    /// Adds or updates pass data in the blackboard.
    /// </summary>
    internal void Add<TPass, TPassData>(TPass pass)
        where TPass : RenderGraphPass<TPassData>
        where TPassData : struct
    {
        var type = typeof(TPass);
        _data[type] = pass;
    }

    /// <summary>
    /// Retrieves pass data from the blackboard.
    /// </summary>
    public ref readonly T Get<T>()
        where T : struct
    {
        ref readonly var data = ref TryGet<T>(out var exist);
        if (!exist)
        {
            throw new KeyNotFoundException($"Pass data of type {typeof(T).Name} not found in blackboard");
        }

        return ref data;
    }

    /// <summary>
    /// Tries to get pass data from the blackboard.
    /// </summary>
    public ref readonly T TryGet<T>(out bool exist)
        where T : struct
    {
        var type = typeof(T);
        if (_data.TryGetValue(type, out var pass)
            && pass is RenderGraphPass<T> typedPass)
        {
            exist = true;
            return ref typedPass.PassData;
        }

        exist = false;
        return ref Unsafe.NullRef<T>();
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
