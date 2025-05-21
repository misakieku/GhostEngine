

namespace Ghost.Entities;

public partial struct World
{
    public readonly void Query<T0>(QueryRefComponent<T0> callback)
        where T0 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            callback(entity, ref pool0.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1>(QueryRefComponent<T0, T1> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2>(QueryRefComponent<T0, T1, T2> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3>(QueryRefComponent<T0, T1, T2, T3> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4>(QueryRefComponent<T0, T1, T2, T3, T4> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5>(QueryRefComponent<T0, T1, T2, T3, T4, T5> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent where T19 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18) && TryGetPool<T19>(out var pool19)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity) && pool19.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity), ref pool19.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent where T19 : struct, IComponent where T20 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18) && TryGetPool<T19>(out var pool19) && TryGetPool<T20>(out var pool20)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity) && pool19.Has(entity) && pool20.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity), ref pool19.GetRef(entity), ref pool20.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent where T19 : struct, IComponent where T20 : struct, IComponent where T21 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18) && TryGetPool<T19>(out var pool19) && TryGetPool<T20>(out var pool20) && TryGetPool<T21>(out var pool21)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity) && pool19.Has(entity) && pool20.Has(entity) && pool21.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity), ref pool19.GetRef(entity), ref pool20.GetRef(entity), ref pool21.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent where T19 : struct, IComponent where T20 : struct, IComponent where T21 : struct, IComponent where T22 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18) && TryGetPool<T19>(out var pool19) && TryGetPool<T20>(out var pool20) && TryGetPool<T21>(out var pool21) && TryGetPool<T22>(out var pool22)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity) && pool19.Has(entity) && pool20.Has(entity) && pool21.Has(entity) && pool22.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity), ref pool19.GetRef(entity), ref pool20.GetRef(entity), ref pool21.GetRef(entity), ref pool22.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent where T19 : struct, IComponent where T20 : struct, IComponent where T21 : struct, IComponent where T22 : struct, IComponent where T23 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18) && TryGetPool<T19>(out var pool19) && TryGetPool<T20>(out var pool20) && TryGetPool<T21>(out var pool21) && TryGetPool<T22>(out var pool22) && TryGetPool<T23>(out var pool23)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity) && pool19.Has(entity) && pool20.Has(entity) && pool21.Has(entity) && pool22.Has(entity) && pool23.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity), ref pool19.GetRef(entity), ref pool20.GetRef(entity), ref pool21.GetRef(entity), ref pool22.GetRef(entity), ref pool23.GetRef(entity));
        }
    }

    public readonly void Query<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> callback)
        where T0 : struct, IComponent where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent where T9 : struct, IComponent where T10 : struct, IComponent where T11 : struct, IComponent where T12 : struct, IComponent where T13 : struct, IComponent where T14 : struct, IComponent where T15 : struct, IComponent where T16 : struct, IComponent where T17 : struct, IComponent where T18 : struct, IComponent where T19 : struct, IComponent where T20 : struct, IComponent where T21 : struct, IComponent where T22 : struct, IComponent where T23 : struct, IComponent where T24 : struct, IComponent
    {
        if (!(TryGetPool<T0>(out var pool0) && TryGetPool<T1>(out var pool1) && TryGetPool<T2>(out var pool2) && TryGetPool<T3>(out var pool3) && TryGetPool<T4>(out var pool4) && TryGetPool<T5>(out var pool5) && TryGetPool<T6>(out var pool6) && TryGetPool<T7>(out var pool7) && TryGetPool<T8>(out var pool8) && TryGetPool<T9>(out var pool9) && TryGetPool<T10>(out var pool10) && TryGetPool<T11>(out var pool11) && TryGetPool<T12>(out var pool12) && TryGetPool<T13>(out var pool13) && TryGetPool<T14>(out var pool14) && TryGetPool<T15>(out var pool15) && TryGetPool<T16>(out var pool16) && TryGetPool<T17>(out var pool17) && TryGetPool<T18>(out var pool18) && TryGetPool<T19>(out var pool19) && TryGetPool<T20>(out var pool20) && TryGetPool<T21>(out var pool21) && TryGetPool<T22>(out var pool22) && TryGetPool<T23>(out var pool23) && TryGetPool<T24>(out var pool24)))
        {
            return;
        }

        for (var i = 0; i < pool0.Count; i++)
        {
			var entity = _entities[i];
            if (!(pool1.Has(entity) && pool2.Has(entity) && pool3.Has(entity) && pool4.Has(entity) && pool5.Has(entity) && pool6.Has(entity) && pool7.Has(entity) && pool8.Has(entity) && pool9.Has(entity) && pool10.Has(entity) && pool11.Has(entity) && pool12.Has(entity) && pool13.Has(entity) && pool14.Has(entity) && pool15.Has(entity) && pool16.Has(entity) && pool17.Has(entity) && pool18.Has(entity) && pool19.Has(entity) && pool20.Has(entity) && pool21.Has(entity) && pool22.Has(entity) && pool23.Has(entity) && pool24.Has(entity)))
            {
                continue;
            }

            callback(entity, ref pool0.GetRef(entity), ref pool1.GetRef(entity), ref pool2.GetRef(entity), ref pool3.GetRef(entity), ref pool4.GetRef(entity), ref pool5.GetRef(entity), ref pool6.GetRef(entity), ref pool7.GetRef(entity), ref pool8.GetRef(entity), ref pool9.GetRef(entity), ref pool10.GetRef(entity), ref pool11.GetRef(entity), ref pool12.GetRef(entity), ref pool13.GetRef(entity), ref pool14.GetRef(entity), ref pool15.GetRef(entity), ref pool16.GetRef(entity), ref pool17.GetRef(entity), ref pool18.GetRef(entity), ref pool19.GetRef(entity), ref pool20.GetRef(entity), ref pool21.GetRef(entity), ref pool22.GetRef(entity), ref pool23.GetRef(entity), ref pool24.GetRef(entity));
        }
    }

}