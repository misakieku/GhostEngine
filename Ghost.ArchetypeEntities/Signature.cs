using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;

namespace Ghost.Entities;

internal struct Signature : IDisposable, IEquatable<Signature>
{
    public UnsafeArray<ComponentData> componentDatas;
    private int _hashCode;

    public Signature(params Span<ComponentData> components)
    {
        componentDatas = new UnsafeArray<ComponentData>(components.Length, Allocator.Persistent);
        componentDatas.CopyFrom(components);

        _hashCode = -1;
        _hashCode = GetHashCode();
    }

    public bool Equals(Signature other)
    {
        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is Signature other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        if (_hashCode != -1)
        {
            return _hashCode;
        }

        unchecked
        {
            _hashCode = Component.GetHashCode(componentDatas.AsSpan());
            return _hashCode;
        }
    }

    public void Dispose()
    {
        componentDatas.Dispose();
    }
}
