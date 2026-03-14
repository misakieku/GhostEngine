namespace Ghost.Ufbx;

public unsafe struct SurfacePoint
{
    private ufbx_surface_point* _ptr;

    internal SurfacePoint(ufbx_surface_point* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool Valid => _ptr->valid;

    public Misaki.HighPerformance.Mathematics.float3 Position => _ptr->position;

    public Misaki.HighPerformance.Mathematics.float3 DerivativeU => _ptr->derivative_u;

    public Misaki.HighPerformance.Mathematics.float3 DerivativeV => _ptr->derivative_v;

    internal ufbx_surface_point* GetUnsafePtr() => _ptr;
}
