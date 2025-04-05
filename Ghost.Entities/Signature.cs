using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;

namespace Ghost.Entities;

internal struct Signature : IDisposable
{
    internal UnsafeArray<ComponentData> _componentDatas;
    private int _hashCode;

    public Signature(params Span<ComponentData> components)
    {
        _componentDatas = new UnsafeArray<ComponentData>(components.Length, Allocator.Persistent);
        _componentDatas.CopyFrom(components);

        _hashCode = -1;
        _hashCode = GetHashCode();
    }

    public override int GetHashCode()
    {
        if (_hashCode != -1)
        {
            return _hashCode;
        }

        unchecked
        {
            _hashCode = Component.GetHashCode(_componentDatas.AsSpan());
            return _hashCode;
        }
    }

    public void Dispose()
    {
        _componentDatas.Dispose();
    }
}
