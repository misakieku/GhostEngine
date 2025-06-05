

using Ghost.Entities.Components;

namespace Ghost.Entities;

public partial class World
{
    public QueryEnumerable<T0> Query<T0>()
        where T0 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0)))
            return default;

        return new QueryEnumerable<T0>(
            this,
            pool0,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1> Query<T0, T1>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1)))
            return default;

        return new QueryEnumerable<T0, T1>(
            this,
            pool0, pool1,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1, T2> Query<T0, T1, T2>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1) && _componentStorage.TryGetPool<T2>(out var pool2)))
            return default;

        return new QueryEnumerable<T0, T1, T2>(
            this,
            pool0, pool1, pool2,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1, T2, T3> Query<T0, T1, T2, T3>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1) && _componentStorage.TryGetPool<T2>(out var pool2) && _componentStorage.TryGetPool<T3>(out var pool3)))
            return default;

        return new QueryEnumerable<T0, T1, T2, T3>(
            this,
            pool0, pool1, pool2, pool3,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1, T2, T3, T4> Query<T0, T1, T2, T3, T4>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1) && _componentStorage.TryGetPool<T2>(out var pool2) && _componentStorage.TryGetPool<T3>(out var pool3) && _componentStorage.TryGetPool<T4>(out var pool4)))
            return default;

        return new QueryEnumerable<T0, T1, T2, T3, T4>(
            this,
            pool0, pool1, pool2, pool3, pool4,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1, T2, T3, T4, T5> Query<T0, T1, T2, T3, T4, T5>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1) && _componentStorage.TryGetPool<T2>(out var pool2) && _componentStorage.TryGetPool<T3>(out var pool3) && _componentStorage.TryGetPool<T4>(out var pool4) && _componentStorage.TryGetPool<T5>(out var pool5)))
            return default;

        return new QueryEnumerable<T0, T1, T2, T3, T4, T5>(
            this,
            pool0, pool1, pool2, pool3, pool4, pool5,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> Query<T0, T1, T2, T3, T4, T5, T6>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1) && _componentStorage.TryGetPool<T2>(out var pool2) && _componentStorage.TryGetPool<T3>(out var pool3) && _componentStorage.TryGetPool<T4>(out var pool4) && _componentStorage.TryGetPool<T5>(out var pool5) && _componentStorage.TryGetPool<T6>(out var pool6)))
            return default;

        return new QueryEnumerable<T0, T1, T2, T3, T4, T5, T6>(
            this,
            pool0, pool1, pool2, pool3, pool4, pool5, pool6,
            pool0.Count);
    }

    public QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> Query<T0, T1, T2, T3, T4, T5, T6, T7>()
        where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData where T7 : struct, IComponentData
    {
        if (!(_componentStorage.TryGetPool<T0>(out var pool0) && _componentStorage.TryGetPool<T1>(out var pool1) && _componentStorage.TryGetPool<T2>(out var pool2) && _componentStorage.TryGetPool<T3>(out var pool3) && _componentStorage.TryGetPool<T4>(out var pool4) && _componentStorage.TryGetPool<T5>(out var pool5) && _componentStorage.TryGetPool<T6>(out var pool6) && _componentStorage.TryGetPool<T7>(out var pool7)))
            return default;

        return new QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7>(
            this,
            pool0, pool1, pool2, pool3, pool4, pool5, pool6, pool7,
            pool0.Count);
    }

}