namespace Ghost.Ufbx;

public unsafe struct CurvePoint
{
    private ufbx_curve_point* _ptr;

    internal CurvePoint(ufbx_curve_point* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool Valid => _ptr->valid;

    public Misaki.HighPerformance.Mathematics.float3 Position => _ptr->position;

    public Misaki.HighPerformance.Mathematics.float3 Derivative => _ptr->derivative;

    internal ufbx_curve_point* GetUnsafePtr() => _ptr;
}
