namespace Ghost.Entities;

public delegate void ForEach<T0>(ref T0 component0)
    where T0 : unmanaged, IComponent;
public delegate void ForEach<T0, T1>(ref T0 component0, ref T1 component1)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent;
public delegate void ForEach<T0, T1, T2>(ref T0 component0, ref T1 component1, ref T2 component2)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent;
public delegate void ForEach<T0, T1, T2, T3>(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent;
public delegate void ForEach<T0, T1, T2, T3, T4>(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent;
public delegate void ForEach<T0, T1, T2, T3, T4, T5>(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent;
public delegate void ForEach<T0, T1, T2, T3, T4, T5, T6>(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent;
public delegate void ForEach<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6, ref T7 component7)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent;

public delegate void ForEachWithEntity<T0>(Entity entity, ref T0 component0)
    where T0 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1>(Entity entity, ref T0 component0, ref T1 component1)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1, T2>(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1, T2, T3>(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1, T2, T3, T4>(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1, T2, T3, T4, T5>(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1, T2, T3, T4, T5, T6>(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent;
public delegate void ForEachWithEntity<T0, T1, T2, T3, T4, T5, T6, T7>(Entity entity, ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6, ref T7 component7)
    where T0 : unmanaged, IComponent where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent;
