

namespace Ghost.Entities;

public delegate void QueryRefComponent<T0>(Entity entity, ref T0 t0Component)
    where T0 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1>(Entity entity, ref T0 t0Component,ref T1 t1Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4, T5>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component,ref T5 t5Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4, T5, T6>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component,ref T5 t5Component,ref T6 t6Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData;
public delegate void QueryRefComponent<T0, T1, T2, T3, T4, T5, T6, T7>(Entity entity, ref T0 t0Component,ref T1 t1Component,ref T2 t2Component,ref T3 t3Component,ref T4 t4Component,ref T5 t5Component,ref T6 t6Component,ref T7 t7Component)
    where T0 : struct, IComponentData where T1 : struct, IComponentData where T2 : struct, IComponentData where T3 : struct, IComponentData where T4 : struct, IComponentData where T5 : struct, IComponentData where T6 : struct, IComponentData where T7 : struct, IComponentData;
