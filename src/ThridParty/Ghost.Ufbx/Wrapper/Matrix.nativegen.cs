namespace Ghost.Ufbx;

public unsafe struct Matrix
{
    private ufbx_matrix* _ptr;

    internal Matrix(ufbx_matrix* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float M00 => _ptr->m00;

    public float M10 => _ptr->m10;

    public float M20 => _ptr->m20;

    public float M01 => _ptr->m01;

    public float M11 => _ptr->m11;

    public float M21 => _ptr->m21;

    public float M02 => _ptr->m02;

    public float M12 => _ptr->m12;

    public float M22 => _ptr->m22;

    public float M03 => _ptr->m03;

    public float M13 => _ptr->m13;

    public float M23 => _ptr->m23;

    internal ufbx_matrix* GetUnsafePtr() => _ptr;
}
