namespace Ghost.Ufbx;

public unsafe struct Camera
{
    private ufbx_camera* _ptr;

    internal Camera(ufbx_camera* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_projection_mode ProjectionMode => _ptr->projection_mode;

    public bool ResolutionIsPixels => _ptr->resolution_is_pixels;

    public Misaki.HighPerformance.Mathematics.float2 Resolution => _ptr->resolution;

    public Misaki.HighPerformance.Mathematics.float2 FieldOfViewDeg => _ptr->field_of_view_deg;

    public Misaki.HighPerformance.Mathematics.float2 FieldOfViewTan => _ptr->field_of_view_tan;

    public float OrthographicExtent => _ptr->orthographic_extent;

    public Misaki.HighPerformance.Mathematics.float2 OrthographicSize => _ptr->orthographic_size;

    public Misaki.HighPerformance.Mathematics.float2 ProjectionPlane => _ptr->projection_plane;

    public float AspectRatio => _ptr->aspect_ratio;

    public float NearPlane => _ptr->near_plane;

    public float FarPlane => _ptr->far_plane;

    public CoordinateAxes ProjectionAxes => new((ufbx_coordinate_axes*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->projection_axes));

    public ufbx_aspect_mode AspectMode => _ptr->aspect_mode;

    public ufbx_aperture_mode ApertureMode => _ptr->aperture_mode;

    public ufbx_gate_fit GateFit => _ptr->gate_fit;

    public ufbx_aperture_format ApertureFormat => _ptr->aperture_format;

    public float FocalLengthMm => _ptr->focal_length_mm;

    public Misaki.HighPerformance.Mathematics.float2 FilmSizeInch => _ptr->film_size_inch;

    public Misaki.HighPerformance.Mathematics.float2 ApertureSizeInch => _ptr->aperture_size_inch;

    public float SqueezeRatio => _ptr->squeeze_ratio;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_camera* GetUnsafePtr() => _ptr;
}
