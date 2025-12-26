using System.Runtime.InteropServices;

namespace Ghost.Shader.Concept;

/// <summary>
/// Material property types supported by the system.
/// </summary>
public enum MaterialPropertyType : byte
{
    Float,
    Float2,
    Float3,
    Float4,
    Int,
    Matrix4x4,
    Texture2D,
    TextureCube
}

/// <summary>
/// Metadata for a material property.
/// </summary>
public readonly struct MaterialPropertyInfo
{
    public readonly string Name;
    public readonly MaterialPropertyType Type;
    public readonly int Offset;
    public readonly int Size;

    public MaterialPropertyInfo(string name, MaterialPropertyType type, int offset, int size)
    {
        Name = name;
        Type = type;
        Offset = offset;
        Size = size;
    }
}

/// <summary>
/// Thread-safe storage for material properties using linear memory layout.
/// Optimized for fast updates and GPU buffer uploads.
/// </summary>
public unsafe sealed class MaterialPropertyBlock : IDisposable
{
    private byte* _data;
    private int _capacity;
    private int _size;
    private readonly object _lock = new();
    private readonly Dictionary<string, MaterialPropertyInfo> _properties = new();

    public int Size => _size;
    public IntPtr DataPtr => (IntPtr)_data;

    public MaterialPropertyBlock(int initialCapacity = 1024)
    {
        _capacity = initialCapacity;
        _data = (byte*)Marshal.AllocHGlobal(_capacity);
        _size = 0;
    }

    ~MaterialPropertyBlock()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_data != null)
        {
            Marshal.FreeHGlobal((IntPtr)_data);
            _data = null;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (_capacity >= required) return;

        int newCapacity = Math.Max(_capacity * 2, required);
        byte* newData = (byte*)Marshal.AllocHGlobal(newCapacity);
        Buffer.MemoryCopy(_data, newData, newCapacity, _size);
        Marshal.FreeHGlobal((IntPtr)_data);
        _data = newData;
        _capacity = newCapacity;
    }

    public void SetFloat(string name, float value)
    {
        lock (_lock)
        {
            var info = GetOrCreateProperty(name, MaterialPropertyType.Float, sizeof(float));
            *(float*)(_data + info.Offset) = value;
        }
    }

    public void SetVector2(string name, float x, float y)
    {
        lock (_lock)
        {
            var info = GetOrCreateProperty(name, MaterialPropertyType.Float2, sizeof(float) * 2);
            float* ptr = (float*)(_data + info.Offset);
            ptr[0] = x;
            ptr[1] = y;
        }
    }

    public void SetVector3(string name, float x, float y, float z)
    {
        lock (_lock)
        {
            var info = GetOrCreateProperty(name, MaterialPropertyType.Float3, sizeof(float) * 3);
            float* ptr = (float*)(_data + info.Offset);
            ptr[0] = x;
            ptr[1] = y;
            ptr[2] = z;
        }
    }

    public void SetVector4(string name, float x, float y, float z, float w)
    {
        lock (_lock)
        {
            var info = GetOrCreateProperty(name, MaterialPropertyType.Float4, sizeof(float) * 4);
            float* ptr = (float*)(_data + info.Offset);
            ptr[0] = x;
            ptr[1] = y;
            ptr[2] = z;
            ptr[3] = w;
        }
    }

    public void SetInt(string name, int value)
    {
        lock (_lock)
        {
            var info = GetOrCreateProperty(name, MaterialPropertyType.Int, sizeof(int));
            *(int*)(_data + info.Offset) = value;
        }
    }

    public void SetMatrix4x4(string name, ReadOnlySpan<float> matrix)
    {
        if (matrix.Length != 16)
            throw new ArgumentException("Matrix must have 16 elements", nameof(matrix));

        lock (_lock)
        {
            var info = GetOrCreateProperty(name, MaterialPropertyType.Matrix4x4, sizeof(float) * 16);
            fixed (float* src = matrix)
            {
                Buffer.MemoryCopy(src, _data + info.Offset, info.Size, info.Size);
            }
        }
    }

    public bool TryGetFloat(string name, out float value)
    {
        lock (_lock)
        {
            if (_properties.TryGetValue(name, out var info) && info.Type == MaterialPropertyType.Float)
            {
                value = *(float*)(_data + info.Offset);
                return true;
            }
            value = 0;
            return false;
        }
    }

    public void CopyTo(byte* destination, int maxSize)
    {
        lock (_lock)
        {
            int copySize = Math.Min(_size, maxSize);
            Buffer.MemoryCopy(_data, destination, maxSize, copySize);
        }
    }

    public void CopyFrom(MaterialPropertyBlock source)
    {
        lock (_lock)
        {
            lock (source._lock)
            {
                EnsureCapacity(source._size);
                Buffer.MemoryCopy(source._data, _data, _capacity, source._size);
                _size = source._size;
                _properties.Clear();
                foreach (var kvp in source._properties)
                {
                    _properties[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    private MaterialPropertyInfo GetOrCreateProperty(string name, MaterialPropertyType type, int size)
    {
        if (_properties.TryGetValue(name, out var existing))
        {
            if (existing.Type != type)
                throw new InvalidOperationException($"Property {name} type mismatch: expected {existing.Type}, got {type}");
            return existing;
        }

        // Align to 16 bytes for GPU compatibility
        int offset = (_size + 15) & ~15;
        int alignedSize = (size + 15) & ~15;
        
        EnsureCapacity(offset + alignedSize);

        var info = new MaterialPropertyInfo(name, type, offset, alignedSize);
        _properties[name] = info;
        _size = offset + alignedSize;

        return info;
    }
}
