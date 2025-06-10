

using Ghost.Entities.Components;
using Ghost.Entities.Query;

namespace Ghost.Entities;

public readonly struct QueryItem<T0>
    where T0 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0)
    {
        _entity = entity;
        _pool0 = pool0;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1>
    where T0 : struct, IComponentData where T1 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1, T2>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);
    public ref T2 Component2 => ref _pool2.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
        c2 = new(ref _pool2.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1, T2, T3>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);
    public ref T2 Component2 => ref _pool2.GetRef(_entity);
    public ref T3 Component3 => ref _pool3.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
        c2 = new(ref _pool2.GetRef(_entity));
        c3 = new(ref _pool3.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1, T2, T3, T4>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);
    public ref T2 Component2 => ref _pool2.GetRef(_entity);
    public ref T3 Component3 => ref _pool3.GetRef(_entity);
    public ref T4 Component4 => ref _pool4.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
        c2 = new(ref _pool2.GetRef(_entity));
        c3 = new(ref _pool3.GetRef(_entity));
        c4 = new(ref _pool4.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1, T2, T3, T4, T5>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly ComponentPool<T5> _pool5;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
        _pool5 = pool5;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);
    public ref T2 Component2 => ref _pool2.GetRef(_entity);
    public ref T3 Component3 => ref _pool3.GetRef(_entity);
    public ref T4 Component4 => ref _pool4.GetRef(_entity);
    public ref T5 Component5 => ref _pool5.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4, out Ref<T5> c5)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
        c2 = new(ref _pool2.GetRef(_entity));
        c3 = new(ref _pool3.GetRef(_entity));
        c4 = new(ref _pool4.GetRef(_entity));
        c5 = new(ref _pool5.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1, T2, T3, T4, T5, T6>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly ComponentPool<T5> _pool5;
    private readonly ComponentPool<T6> _pool6;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, ComponentPool<T6> pool6)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
        _pool5 = pool5;
        _pool6 = pool6;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);
    public ref T2 Component2 => ref _pool2.GetRef(_entity);
    public ref T3 Component3 => ref _pool3.GetRef(_entity);
    public ref T4 Component4 => ref _pool4.GetRef(_entity);
    public ref T5 Component5 => ref _pool5.GetRef(_entity);
    public ref T6 Component6 => ref _pool6.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4, out Ref<T5> c5, out Ref<T6> c6)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
        c2 = new(ref _pool2.GetRef(_entity));
        c3 = new(ref _pool3.GetRef(_entity));
        c4 = new(ref _pool4.GetRef(_entity));
        c5 = new(ref _pool5.GetRef(_entity));
        c6 = new(ref _pool6.GetRef(_entity));
    }
}

public readonly struct QueryItem<T0, T1, T2, T3, T4, T5, T6, T7>
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData where T7 : struct, IComponentData
{
    private readonly Entity _entity;
    private readonly ComponentPool<T0> _pool0;
    private readonly ComponentPool<T1> _pool1;
    private readonly ComponentPool<T2> _pool2;
    private readonly ComponentPool<T3> _pool3;
    private readonly ComponentPool<T4> _pool4;
    private readonly ComponentPool<T5> _pool5;
    private readonly ComponentPool<T6> _pool6;
    private readonly ComponentPool<T7> _pool7;

    internal QueryItem(Entity entity, ComponentPool<T0> pool0, ComponentPool<T1> pool1, ComponentPool<T2> pool2, ComponentPool<T3> pool3, ComponentPool<T4> pool4, ComponentPool<T5> pool5, ComponentPool<T6> pool6, ComponentPool<T7> pool7)
    {
        _entity = entity;
        _pool0 = pool0;
        _pool1 = pool1;
        _pool2 = pool2;
        _pool3 = pool3;
        _pool4 = pool4;
        _pool5 = pool5;
        _pool6 = pool6;
        _pool7 = pool7;
    }

    public Entity Entity => _entity;

    public ref T0 Component0 => ref _pool0.GetRef(_entity);
    public ref T1 Component1 => ref _pool1.GetRef(_entity);
    public ref T2 Component2 => ref _pool2.GetRef(_entity);
    public ref T3 Component3 => ref _pool3.GetRef(_entity);
    public ref T4 Component4 => ref _pool4.GetRef(_entity);
    public ref T5 Component5 => ref _pool5.GetRef(_entity);
    public ref T6 Component6 => ref _pool6.GetRef(_entity);
    public ref T7 Component7 => ref _pool7.GetRef(_entity);

    // Deconstruct into tuple-like values
    public void Deconstruct(out Entity entity, out Ref<T0> c0, out Ref<T1> c1, out Ref<T2> c2, out Ref<T3> c3, out Ref<T4> c4, out Ref<T5> c5, out Ref<T6> c6, out Ref<T7> c7)
    {
        entity = _entity;
        c0 = new(ref _pool0.GetRef(_entity));
        c1 = new(ref _pool1.GetRef(_entity));
        c2 = new(ref _pool2.GetRef(_entity));
        c3 = new(ref _pool3.GetRef(_entity));
        c4 = new(ref _pool4.GetRef(_entity));
        c5 = new(ref _pool5.GetRef(_entity));
        c6 = new(ref _pool6.GetRef(_entity));
        c7 = new(ref _pool7.GetRef(_entity));
    }
}

