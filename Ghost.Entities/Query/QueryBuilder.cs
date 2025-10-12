using Ghost.Core;

namespace Ghost.Entities.Query;

public struct QueryBuilder
{
    private QueryFilter _filter;

    public QueryBuilder()
    {
        _filter = new QueryFilter();
    }

    public QueryBuilder WithAll<T>()
    {
        _filter._all.Add(TypeHandle.Get<T>());
        return this;
    }

    public QueryBuilder WithAny<T>()
    {
        _filter._any.Add(TypeHandle.Get<T>());
        return this;
    }

    public QueryBuilder WithAbsent<T>()
    {
        _filter._absent.Add(TypeHandle.Get<T>());
        return this;
    }

    public QueryBuilder WithDisabled<T>()
    {
        _filter._disabled.Add(TypeHandle.Get<T>());
        return this;
    }

    public readonly QueryFilter Build()
    {
        return _filter;
    }
}