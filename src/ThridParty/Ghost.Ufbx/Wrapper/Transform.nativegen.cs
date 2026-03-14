namespace Ghost.Ufbx;

public unsafe struct Transform
{
    private ufbx_transform* _ptr;

    internal Transform(ufbx_transform* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Misaki.HighPerformance.Mathematics.float3x4 TransformToMatrix()
    {
        return Api.ufbx_transform_to_matrix(_ptr);
    }

    public Misaki.HighPerformance.Mathematics.float3 Translation => _ptr->translation;

    public Misaki.HighPerformance.Mathematics.quaternion Rotation => _ptr->rotation;

    public Misaki.HighPerformance.Mathematics.float3 Scale => _ptr->scale;

    internal ufbx_transform* GetUnsafePtr() => _ptr;
}
