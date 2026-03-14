namespace Ghost.Ufbx;

public unsafe ref struct Element
{
    private ufbx_element* _ptr;

    internal Element(ufbx_element* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Element GetPropElement(Prop prop, ufbx_element_type type)
    {
        return new(Api.ufbx_get_prop_element(_ptr, prop.GetUnsafePtr(), type));
    }

    public Element FindPropElementLen(sbyte* name, nuint nameLen, ufbx_element_type type)
    {
        return new(Api.ufbx_find_prop_element_len(_ptr, name, nameLen, type));
    }

    public Element FindPropElement(sbyte* name, ufbx_element_type type)
    {
        return new(Api.ufbx_find_prop_element(_ptr, name, type));
    }

    public Unknown AsUnknown()
    {
        return new(Api.ufbx_as_unknown(_ptr));
    }

    public Node AsNode()
    {
        return new(Api.ufbx_as_node(_ptr));
    }

    public Mesh AsMesh()
    {
        return new(Api.ufbx_as_mesh(_ptr));
    }

    public Light AsLight()
    {
        return new(Api.ufbx_as_light(_ptr));
    }

    public Camera AsCamera()
    {
        return new(Api.ufbx_as_camera(_ptr));
    }

    public Bone AsBone()
    {
        return new(Api.ufbx_as_bone(_ptr));
    }

    public Empty AsEmpty()
    {
        return new(Api.ufbx_as_empty(_ptr));
    }

    public LineCurve AsLineCurve()
    {
        return new(Api.ufbx_as_line_curve(_ptr));
    }

    public NurbsCurve AsNurbsCurve()
    {
        return new(Api.ufbx_as_nurbs_curve(_ptr));
    }

    public NurbsSurface AsNurbsSurface()
    {
        return new(Api.ufbx_as_nurbs_surface(_ptr));
    }

    public NurbsTrimSurface AsNurbsTrimSurface()
    {
        return new(Api.ufbx_as_nurbs_trim_surface(_ptr));
    }

    public NurbsTrimBoundary AsNurbsTrimBoundary()
    {
        return new(Api.ufbx_as_nurbs_trim_boundary(_ptr));
    }

    public ProceduralGeometry AsProceduralGeometry()
    {
        return new(Api.ufbx_as_procedural_geometry(_ptr));
    }

    public StereoCamera AsStereoCamera()
    {
        return new(Api.ufbx_as_stereo_camera(_ptr));
    }

    public CameraSwitcher AsCameraSwitcher()
    {
        return new(Api.ufbx_as_camera_switcher(_ptr));
    }

    public Marker AsMarker()
    {
        return new(Api.ufbx_as_marker(_ptr));
    }

    public LodGroup AsLodGroup()
    {
        return new(Api.ufbx_as_lod_group(_ptr));
    }

    public SkinDeformer AsSkinDeformer()
    {
        return new(Api.ufbx_as_skin_deformer(_ptr));
    }

    public SkinCluster AsSkinCluster()
    {
        return new(Api.ufbx_as_skin_cluster(_ptr));
    }

    public BlendDeformer AsBlendDeformer()
    {
        return new(Api.ufbx_as_blend_deformer(_ptr));
    }

    public BlendChannel AsBlendChannel()
    {
        return new(Api.ufbx_as_blend_channel(_ptr));
    }

    public BlendShape AsBlendShape()
    {
        return new(Api.ufbx_as_blend_shape(_ptr));
    }

    public CacheDeformer AsCacheDeformer()
    {
        return new(Api.ufbx_as_cache_deformer(_ptr));
    }

    public CacheFile AsCacheFile()
    {
        return new(Api.ufbx_as_cache_file(_ptr));
    }

    public Material AsMaterial()
    {
        return new(Api.ufbx_as_material(_ptr));
    }

    public Texture AsTexture()
    {
        return new(Api.ufbx_as_texture(_ptr));
    }

    public Video AsVideo()
    {
        return new(Api.ufbx_as_video(_ptr));
    }

    public Shader AsShader()
    {
        return new(Api.ufbx_as_shader(_ptr));
    }

    public ShaderBinding AsShaderBinding()
    {
        return new(Api.ufbx_as_shader_binding(_ptr));
    }

    public AnimStack AsAnimStack()
    {
        return new(Api.ufbx_as_anim_stack(_ptr));
    }

    public AnimLayer AsAnimLayer()
    {
        return new(Api.ufbx_as_anim_layer(_ptr));
    }

    public AnimValue AsAnimValue()
    {
        return new(Api.ufbx_as_anim_value(_ptr));
    }

    public AnimCurve AsAnimCurve()
    {
        return new(Api.ufbx_as_anim_curve(_ptr));
    }

    public DisplayLayer AsDisplayLayer()
    {
        return new(Api.ufbx_as_display_layer(_ptr));
    }

    public SelectionSet AsSelectionSet()
    {
        return new(Api.ufbx_as_selection_set(_ptr));
    }

    public SelectionNode AsSelectionNode()
    {
        return new(Api.ufbx_as_selection_node(_ptr));
    }

    public Character AsCharacter()
    {
        return new(Api.ufbx_as_character(_ptr));
    }

    public Constraint AsConstraint()
    {
        return new(Api.ufbx_as_constraint(_ptr));
    }

    public AudioLayer AsAudioLayer()
    {
        return new(Api.ufbx_as_audio_layer(_ptr));
    }

    public AudioClip AsAudioClip()
    {
        return new(Api.ufbx_as_audio_clip(_ptr));
    }

    public Pose AsPose()
    {
        return new(Api.ufbx_as_pose(_ptr));
    }

    public MetadataObject AsMetadataObject()
    {
        return new(Api.ufbx_as_metadata_object(_ptr));
    }

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    public ufbx_element_type Type => _ptr->type;

    public ReadOnlySpan<ufbx_connection> ConnectionsSrc => _ptr->connections_src.data == null ? ReadOnlySpan<ufbx_connection>.Empty : new ReadOnlySpan<ufbx_connection>(_ptr->connections_src.data, checked((int)_ptr->connections_src.count));

    public ReadOnlySpan<ufbx_connection> ConnectionsDst => _ptr->connections_dst.data == null ? ReadOnlySpan<ufbx_connection>.Empty : new ReadOnlySpan<ufbx_connection>(_ptr->connections_dst.data, checked((int)_ptr->connections_dst.count));

    public bool HasDomNode => _ptr->dom_node != null;
    public DomNode DomNode => _ptr->dom_node != null ? new(_ptr->dom_node) : throw new InvalidOperationException("DomNode is null.");

    public bool HasScene => _ptr->scene != null;
    public Scene Scene => _ptr->scene != null ? new(_ptr->scene) : throw new InvalidOperationException("Scene is null.");

    internal ufbx_element* GetUnsafePtr() => _ptr;
}
