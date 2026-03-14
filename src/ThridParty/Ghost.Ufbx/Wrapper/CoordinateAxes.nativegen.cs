namespace Ghost.Ufbx;

public unsafe struct CoordinateAxes
{
    private ufbx_coordinate_axes* _ptr;

    internal CoordinateAxes(ufbx_coordinate_axes* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_coordinate_axis Right => _ptr->right;

    public ufbx_coordinate_axis Up => _ptr->up;

    public ufbx_coordinate_axis Front => _ptr->front;

    internal ufbx_coordinate_axes* GetUnsafePtr() => _ptr;
}
