using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.Core;

/// <summary>
/// Resolves scene-level asset, material-palette, and entity-target references when applying baked scene field records at runtime.
/// </summary>
public interface ISceneReferenceResolver
{
    Handle<T> ResolveAssetHandle<T>(int assetDependencyIndex);
    int ResolveMaterialPalette(ReadOnlySpan<int> materialDependencyIndices);
    void ResolveEntityTarget(SceneEntityTarget target, out int entityId, out int entityGeneration);
}

/// <summary>
/// Zero-allocation reader over a single baked <see cref="SceneFieldRecord"/> consumed by AOT-generated component and volume deserializers.
/// </summary>
public readonly ref struct SceneFieldReader
{
    private readonly SceneFieldRecord _record;
    private readonly ReadOnlySpan<byte> _dataBlob;
    private readonly ReadOnlySpan<int> _materialDependencyIndices;
    private readonly ISceneReferenceResolver? _resolver;

    public SceneFieldReader(in SceneFieldRecord record, ReadOnlySpan<byte> dataBlob, ReadOnlySpan<int> materialDependencyIndices, ISceneReferenceResolver? resolver = null)
    {
        _record = record;
        _dataBlob = dataBlob;
        _materialDependencyIndices = materialDependencyIndices;
        _resolver = resolver;
    }

    public SceneFieldValueKind Kind => _record.kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBool(out bool value)
    {
        switch (_record.kind)
        {
            case SceneFieldValueKind.Bool:
            case SceneFieldValueKind.Int64:
            case SceneFieldValueKind.UInt64:
                value = _record.scalarValue != 0;
                return true;
            default:
                value = false;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt64(out long value)
    {
        switch (_record.kind)
        {
            case SceneFieldValueKind.Int64:
            case SceneFieldValueKind.Bool:
                value = _record.scalarValue;
                return true;
            case SceneFieldValueKind.UInt64:
                value = unchecked((long)(ulong)_record.scalarValue);
                return true;
            case SceneFieldValueKind.Double:
                value = (long)BitConverter.Int64BitsToDouble(_record.scalarValue);
                return true;
            default:
                value = 0;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt64(out ulong value)
    {
        switch (_record.kind)
        {
            case SceneFieldValueKind.UInt64:
                value = unchecked((ulong)_record.scalarValue);
                return true;
            case SceneFieldValueKind.Int64:
            case SceneFieldValueKind.Bool:
                value = unchecked((ulong)_record.scalarValue);
                return true;
            case SceneFieldValueKind.Double:
                value = (ulong)BitConverter.Int64BitsToDouble(_record.scalarValue);
                return true;
            default:
                value = 0;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadDouble(out double value)
    {
        switch (_record.kind)
        {
            case SceneFieldValueKind.Double:
                value = BitConverter.Int64BitsToDouble(_record.scalarValue);
                return true;
            case SceneFieldValueKind.Int64:
            case SceneFieldValueKind.Bool:
                value = _record.scalarValue;
                return true;
            case SceneFieldValueKind.UInt64:
                value = unchecked((ulong)_record.scalarValue);
                return true;
            default:
                value = 0.0;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadSingle(out float value)
    {
        if (TryReadDouble(out var d))
        {
            value = (float)d;
            return true;
        }

        value = 0.0f;
        return false;
    }

    public bool TryReadString(out string value)
    {
        if (_record.kind == SceneFieldValueKind.String && _record.payloadOffset >= 0 && _record.payloadCount >= 0 && _record.payloadOffset + _record.payloadCount <= _dataBlob.Length)
        {
            value = _record.payloadCount == 0 ? string.Empty : Encoding.UTF8.GetString(_dataBlob.Slice(_record.payloadOffset, _record.payloadCount));
            return true;
        }

        value = string.Empty;
        return false;
    }

    public bool TryReadSingles(Span<float> dst)
    {
        if (_record.payloadCount < dst.Length || _record.payloadOffset < 0)
        {
            return false;
        }

        if (_record.kind == SceneFieldValueKind.FloatArray)
        {
            var byteCount = dst.Length * sizeof(float);
            if (_record.payloadOffset + byteCount > _dataBlob.Length)
            {
                return false;
            }

            var floats = MemoryMarshal.Cast<byte, float>(_dataBlob.Slice(_record.payloadOffset, byteCount));
            floats.CopyTo(dst);
            return true;
        }

        if (_record.kind == SceneFieldValueKind.IntArray)
        {
            var byteCount = dst.Length * sizeof(long);
            if (_record.payloadOffset + byteCount > _dataBlob.Length)
            {
                return false;
            }

            var ints = MemoryMarshal.Cast<byte, long>(_dataBlob.Slice(_record.payloadOffset, byteCount));
            for (var i = 0; i < dst.Length; i++)
            {
                dst[i] = ints[i];
            }

            return true;
        }

        return false;
    }

    public bool TryReadInt32s(Span<int> dst)
    {
        if (_record.payloadCount < dst.Length || _record.payloadOffset < 0)
        {
            return false;
        }

        if (_record.kind == SceneFieldValueKind.IntArray)
        {
            var byteCount = dst.Length * sizeof(long);
            if (_record.payloadOffset + byteCount > _dataBlob.Length)
            {
                return false;
            }

            var ints = MemoryMarshal.Cast<byte, long>(_dataBlob.Slice(_record.payloadOffset, byteCount));
            for (var i = 0; i < dst.Length; i++)
            {
                dst[i] = unchecked((int)ints[i]);
            }

            return true;
        }

        if (_record.kind == SceneFieldValueKind.FloatArray)
        {
            var byteCount = dst.Length * sizeof(float);
            if (_record.payloadOffset + byteCount > _dataBlob.Length)
            {
                return false;
            }

            var floats = MemoryMarshal.Cast<byte, float>(_dataBlob.Slice(_record.payloadOffset, byteCount));
            for (var i = 0; i < dst.Length; i++)
            {
                dst[i] = (int)floats[i];
            }

            return true;
        }

        return false;
    }

    public bool TryReadUInt32s(Span<uint> dst)
    {
        if (_record.payloadCount < dst.Length || _record.payloadOffset < 0)
        {
            return false;
        }

        if (_record.kind == SceneFieldValueKind.IntArray)
        {
            var byteCount = dst.Length * sizeof(long);
            if (_record.payloadOffset + byteCount > _dataBlob.Length)
            {
                return false;
            }

            var ints = MemoryMarshal.Cast<byte, long>(_dataBlob.Slice(_record.payloadOffset, byteCount));
            for (var i = 0; i < dst.Length; i++)
            {
                dst[i] = unchecked((uint)ints[i]);
            }

            return true;
        }

        if (_record.kind == SceneFieldValueKind.FloatArray)
        {
            var byteCount = dst.Length * sizeof(float);
            if (_record.payloadOffset + byteCount > _dataBlob.Length)
            {
                return false;
            }

            var floats = MemoryMarshal.Cast<byte, float>(_dataBlob.Slice(_record.payloadOffset, byteCount));
            for (var i = 0; i < dst.Length; i++)
            {
                dst[i] = (uint)floats[i];
            }

            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<T> ResolveAssetHandle<T>()
    {
        if (_record.kind == SceneFieldValueKind.AssetHandle && _record.payloadOffset >= 0 && _resolver != null)
        {
            return _resolver.ResolveAssetHandle<T>(_record.payloadOffset);
        }

        return Handle<T>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ResolveMaterialPalette()
    {
        if (_record.kind != SceneFieldValueKind.MaterialPalette || _resolver == null || _record.payloadCount <= 0)
        {
            return -1;
        }

        if (_record.payloadOffset < 0 || _record.payloadOffset + _record.payloadCount > _materialDependencyIndices.Length)
        {
            return -1;
        }

        return _resolver.ResolveMaterialPalette(_materialDependencyIndices.Slice(_record.payloadOffset, _record.payloadCount));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResolveEntityRaw(out int entityId, out int entityGeneration)
    {
        if (_record.kind == SceneFieldValueKind.EntityRef && _record.entityTarget.IsValid && _resolver != null)
        {
            _resolver.ResolveEntityTarget(_record.entityTarget, out entityId, out entityGeneration);
            return;
        }

        entityId = -1;
        entityGeneration = 0;
    }
}
