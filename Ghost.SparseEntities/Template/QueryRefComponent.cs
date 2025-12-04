

using Ghost.SparseEntities.Components;

namespace Ghost.SparseEntities;

public delegate void QueryRefComponent<T0>(Entity entity, ref T0 t0Component)
    where T0 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1>(Entity entity, ref T0 t0Component,ref T1 t1Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData where T4 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4, T5>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component,ref T5 t5Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData where T4 : unmanaged, IComponentData where T5 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4, T5, T6>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component,ref T5 t5Component,ref T6 t6Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData where T4 : unmanaged, IComponentData where T5 : unmanaged, IComponentData where T6 : unmanaged, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component,ref T5 t5Component,ref T6 t6Component,ref T7 t7Component)
    where T0 : unmanaged, IComponentData where T1 : unmanaged, IComponentData where T2 : unmanaged, IComponentData where T3 : unmanaged, IComponentData where T4 : unmanaged, IComponentData where T5 : unmanaged, IComponentData where T6 : unmanaged, IComponentData where T7 : unmanaged, IComponentData;
