namespace Ghost.Ufbx;

public unsafe struct SceneSettings
{
    private ufbx_scene_settings* _ptr;

    internal SceneSettings(ufbx_scene_settings* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public CoordinateAxes Axes => new((ufbx_coordinate_axes*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->axes));

    public float UnitMeters => _ptr->unit_meters;

    public double FramesPerSecond => _ptr->frames_per_second;

    public Misaki.HighPerformance.Mathematics.float3 AmbientColor => _ptr->ambient_color;

    public ReadOnlySpan<byte> DefaultCameraBytes => NativeWrapperHelpers.AsByteSpan(_ptr->default_camera);
    public string DefaultCamera => NativeWrapperHelpers.GetString(_ptr->default_camera);

    public ufbx_time_mode TimeMode => _ptr->time_mode;

    public ufbx_time_protocol TimeProtocol => _ptr->time_protocol;

    public ufbx_snap_mode SnapMode => _ptr->snap_mode;

    public ufbx_coordinate_axis OriginalAxisUp => _ptr->original_axis_up;

    public float OriginalUnitMeters => _ptr->original_unit_meters;

    internal ufbx_scene_settings* GetUnsafePtr() => _ptr;
}
