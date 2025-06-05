

using Ghost.Entities.Components;
using Ghost.Entities.Query;
using Ghost.Entities.Utilities;
using Misaki.HighPerformance.Unsafe.Collections;

namespace Ghost.Entities;

public struct QueryEnumerable<T0>
    where T0 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, int count)
    {
        _world = world;

        _pool0 = pool0;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0>(_entities[_index], _pool0);
            return true;
        }
    }

	public readonly QueryEnumerable<T0> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1>
    where T0 : struct, IComponentData where T1 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1>(_entities[_index], _pool0, _pool1);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1, T2>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
        _filters._all.Add(TypeHandle.Get<T2>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _pool2, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;
        private readonly ComponentPool<T2> _pool2;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1, T2> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;
            _pool2 = pool2;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1, T2>(_entities[_index], _pool0, _pool1, _pool2);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1, T2> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1, T2, T3>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
        _filters._all.Add(TypeHandle.Get<T2>());
        _filters._all.Add(TypeHandle.Get<T3>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _pool2, _pool3, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;
        private readonly ComponentPool<T2> _pool2;
        private readonly ComponentPool<T3> _pool3;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1, T2, T3> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;
            _pool2 = pool2;
            _pool3 = pool3;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1, T2, T3>(_entities[_index], _pool0, _pool1, _pool2, _pool3);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1, T2, T3> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1, T2, T3, T4>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
        _filters._all.Add(TypeHandle.Get<T2>());
        _filters._all.Add(TypeHandle.Get<T3>());
        _filters._all.Add(TypeHandle.Get<T4>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _pool2, _pool3, _pool4, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;
        private readonly ComponentPool<T2> _pool2;
        private readonly ComponentPool<T3> _pool3;
        private readonly ComponentPool<T4> _pool4;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1, T2, T3, T4> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;
            _pool2 = pool2;
            _pool3 = pool3;
            _pool4 = pool4;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1, T2, T3, T4>(_entities[_index], _pool0, _pool1, _pool2, _pool3, _pool4);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1, T2, T3, T4, T5>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly ComponentPool<T5> _pool5;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
        _pool5 = pool5;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
        _filters._all.Add(TypeHandle.Get<T2>());
        _filters._all.Add(TypeHandle.Get<T3>());
        _filters._all.Add(TypeHandle.Get<T4>());
        _filters._all.Add(TypeHandle.Get<T5>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _pool2, _pool3, _pool4, _pool5, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;
        private readonly ComponentPool<T2> _pool2;
        private readonly ComponentPool<T3> _pool3;
        private readonly ComponentPool<T4> _pool4;
        private readonly ComponentPool<T5> _pool5;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1, T2, T3, T4, T5> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;
            _pool2 = pool2;
            _pool3 = pool3;
            _pool4 = pool4;
            _pool5 = pool5;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1, T2, T3, T4, T5>(_entities[_index], _pool0, _pool1, _pool2, _pool3, _pool4, _pool5);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1, T2, T3, T4, T5, T6>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly ComponentPool<T5> _pool5;
    private readonly ComponentPool<T6> _pool6;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, ComponentPool<T6> pool6, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
        _pool5 = pool5;
        _pool6 = pool6;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
        _filters._all.Add(TypeHandle.Get<T2>());
        _filters._all.Add(TypeHandle.Get<T3>());
        _filters._all.Add(TypeHandle.Get<T4>());
        _filters._all.Add(TypeHandle.Get<T5>());
        _filters._all.Add(TypeHandle.Get<T6>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _pool2, _pool3, _pool4, _pool5, _pool6, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;
        private readonly ComponentPool<T2> _pool2;
        private readonly ComponentPool<T3> _pool3;
        private readonly ComponentPool<T4> _pool4;
        private readonly ComponentPool<T5> _pool5;
        private readonly ComponentPool<T6> _pool6;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1, T2, T3, T4, T5, T6> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, ComponentPool<T6> pool6, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;
            _pool2 = pool2;
            _pool3 = pool3;
            _pool4 = pool4;
            _pool5 = pool5;
            _pool6 = pool6;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1, T2, T3, T4, T5, T6>(_entities[_index], _pool0, _pool1, _pool2, _pool3, _pool4, _pool5, _pool6);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

public struct QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData where T7 : struct, IComponentData
{
    private readonly World _world;

    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly ComponentPool<T5> _pool5;
    private readonly ComponentPool<T6> _pool6;
    private readonly ComponentPool<T7> _pool7;
    private readonly int _count;

    private QueryFilter _filters;
    internal readonly QueryFilter Filters => _filters;

    internal QueryEnumerable(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, ComponentPool<T6> pool6, ComponentPool<T7> pool7, int count)
    {
        _world = world;

        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
        _pool5 = pool5;
        _pool6 = pool6;
        _pool7 = pool7;

        _count = count;
        
        _filters = new();
        _filters._all.Add(TypeHandle.Get<T0>());
        _filters._all.Add(TypeHandle.Get<T1>());
        _filters._all.Add(TypeHandle.Get<T2>());
        _filters._all.Add(TypeHandle.Get<T3>());
        _filters._all.Add(TypeHandle.Get<T4>());
        _filters._all.Add(TypeHandle.Get<T5>());
        _filters._all.Add(TypeHandle.Get<T6>());
        _filters._all.Add(TypeHandle.Get<T7>());
    }

    public readonly Enumerator GetEnumerator() => new (_world, _pool0, _pool1, _pool2, _pool3, _pool4, _pool5, _pool6, _pool7, _count, _filters);

    public ref struct Enumerator
    {
        private readonly World _world;
        private readonly ReadOnlySpan<Entity> _entities;

        private readonly ComponentPool<T0> _pool0;
        private readonly ComponentPool<T1> _pool1;
        private readonly ComponentPool<T2> _pool2;
        private readonly ComponentPool<T3> _pool3;
        private readonly ComponentPool<T4> _pool4;
        private readonly ComponentPool<T5> _pool5;
        private readonly ComponentPool<T6> _pool6;
        private readonly ComponentPool<T7> _pool7;

        private int _index;
        private readonly int _count;

        private BitSet _filterMask;
        
        public QueryItem<T0, T1, T2, T3, T4, T5, T6, T7> Current
        {
            get;
            private set;
        }

        internal Enumerator(World world, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, ComponentPool<T6> pool6, ComponentPool<T7> pool7, int count, QueryFilter filters)
        {
            _world = world;
            _entities = _world.EntityManager.Entities;

            _pool0 = pool0;
            _pool1 = pool1;
            _pool2 = pool2;
            _pool3 = pool3;
            _pool4 = pool4;
            _pool5 = pool5;
            _pool6 = pool6;
            _pool7 = pool7;

            _count = count;
            _index = -1;
            filters.ComputeFilterBitMask(_world, ref _filterMask);

            Current = default;
        }

        public bool MoveNext()
        {
            _index = _filterMask.NextSetBit(_index + 1);
            if (_index < 0 || _index >= _world.EntityManager.EntityCount)
            {
                return false;
            }

            Current = new QueryItem<T0, T1, T2, T3, T4, T5, T6, T7>(_entities[_index], _pool0, _pool1, _pool2, _pool3, _pool4, _pool5, _pool6, _pool7);
            return true;
        }
    }

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAll<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAny<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAbsent<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithDisabled<TComponent0>()
		where TComponent0 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAll<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAny<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAbsent<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithDisabled<TComponent0, TComponent1>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        return this;
	}

	public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAll<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._all.Add(TypeHandle.Get<TComponent0>());
        _filters._all.Add(TypeHandle.Get<TComponent1>());
        _filters._all.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAny<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._any.Add(TypeHandle.Get<TComponent0>());
        _filters._any.Add(TypeHandle.Get<TComponent1>());
        _filters._any.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithAbsent<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._absent.Add(TypeHandle.Get<TComponent0>());
        _filters._absent.Add(TypeHandle.Get<TComponent1>());
        _filters._absent.Add(TypeHandle.Get<TComponent2>());
        return this;
	}

    public readonly QueryEnumerable<T0, T1, T2, T3, T4, T5, T6, T7> WithDisabled<TComponent0, TComponent1, TComponent2>()
		where TComponent0 : struct, IComponentData where TComponent1 : struct, IComponentData where TComponent2 : struct, IComponentData
	{
        _filters._disabled.Add(TypeHandle.Get<TComponent0>());
        _filters._disabled.Add(TypeHandle.Get<TComponent1>());
        _filters._disabled.Add(TypeHandle.Get<TComponent2>());
        return this;
	}
}

