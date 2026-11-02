using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Blackboard for sharing data between render passes.
/// </summary>
public sealed class RenderGraphBlackboard
{
    private readonly Dictionary<Type, RenderGraphPass> _data = new(16);
    private readonly Dictionary<Type, int> _dataOffset = new(16);
    private byte[] _dataBuffer = new byte[1024];
    private int _currentOffset = 0;

    /// <summary>
    /// Adds or updates data in the blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the data to add or update.</typeparam>
    /// <param name="data">The data to add or update.</param>
    public void Add<T>(ref T data)
        where T : unmanaged
    {
        var type = typeof(T);
        var size = Unsafe.SizeOf<T>();
        if (_dataOffset.TryGetValue(type, out var offset))
        {
            // Update existing data
            Unsafe.CopyBlockUnaligned(ref _dataBuffer[offset], in Unsafe.As<T, byte>(ref data), (uint)size);
        }
        else
        {
            // Add new data
            offset = _currentOffset;
            if (offset + size > _dataBuffer.Length)
            {
                Array.Resize(ref _dataBuffer, Math.Max(_dataBuffer.Length * 2, offset + size));
            }

            Unsafe.CopyBlockUnaligned(ref _dataBuffer[offset], in Unsafe.As<T, byte>(ref data), (uint)size);
            
            _currentOffset += size;
            _dataOffset[type] = offset;
        }
    }

    /// <summary>
    /// Adds or updates pass data in the blackboard.
    /// </summary>
    /// <typeparam name="TPass">The type of the render graph pass.</typeparam>
    /// <typeparam name="TPassData">The type of the pass data.</typeparam>
    /// <param name="pass">The render graph pass containing the data.</param>
    internal void AddPassData<TPass, TPassData>(TPass pass)
        where TPass : RenderGraphPass<TPassData>
        where TPassData : struct
    {
        var type = typeof(TPassData);
        _data[type] = pass;
    }

    /// <summary>
    /// Retrieves data from the blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the data to retrieve.</typeparam>
    /// <returns>A reference to the data of the specified type.</returns>
    public ref readonly T Get<T>()
        where T : unmanaged
    {
        var type = typeof(T);
        if (_dataOffset.TryGetValue(type, out var offset))
        {
            return ref Unsafe.As<byte, T>(ref _dataBuffer[offset]);
        }

        throw new KeyNotFoundException($"Data of type {typeof(T).Name} not found in blackboard");
    }

    /// <summary>
    /// Tries to get data from the blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the data to retrieve.</typeparam>
    /// <param name="exist">Indicates whether the data exists in the blackboard.</param>
    /// <returns>A reference to the data of the specified type, or a null reference if it does not exist.</returns>
    public ref readonly T TryGet<T>(out bool exist)
        where T : unmanaged
    {
        var type = typeof(T);
        if (_dataOffset.TryGetValue(type, out var offset))
        {
            exist = true;
            return ref Unsafe.As<byte, T>(ref _dataBuffer[offset]);
        }

        exist = false;
        return ref Unsafe.NullRef<T>();
    }

    /// <summary>
    /// Retrieves pass data from the blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the pass data to retrieve.</typeparam>
    /// <returns>A reference to the pass data of the specified type.</returns>
    public ref readonly T GetPassData<T>()
        where T : struct
    {
        ref readonly var data = ref TryGetPassData<T>(out var exist);
        if (!exist)
        {
            throw new KeyNotFoundException($"Pass data of type {typeof(T).Name} not found in blackboard");
        }

        return ref data;
    }

    /// <summary>
    /// Tries to get pass data from the blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the pass data to retrieve.</typeparam>
    /// <param name="exist">Indicates whether the pass data exists in the blackboard.</param>
    /// <returns>A reference to the pass data of the specified type, or a null reference if it does not exist.</returns>
    public ref readonly T TryGetPassData<T>(out bool exist)
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
    public void Reset()
    {
        _data.Clear();
        _dataOffset.Clear();
        _currentOffset = 0;
    }
}
