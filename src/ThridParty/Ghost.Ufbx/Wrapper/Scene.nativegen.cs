namespace Ghost.Ufbx;

public unsafe class Scene : IDisposable
{
    private ufbx_scene* _ptr;

    internal Scene(ufbx_scene* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.ufbx_free_scene(_ptr);
            _ptr = null;
        }
    }

    public static Scene LoadMemory(ReadOnlySpan<byte> data, in ufbx_load_opts options = default)
    {
        var optionsLocal = options;
        ufbx_error error = default;
        fixed (byte* dataPtr = data)
        {
        var value = Api.ufbx_load_memory(dataPtr, (nuint)data.Length, &optionsLocal, &error);
        if (value == null)
        {
            throw new InvalidOperationException(NativeWrapperHelpers.GetString(error.description));
        }
        return new(value);
        }
    }

    public static Scene LoadFile(sbyte* filename, LoadOpts opts, Error error)
    {
        return new(Api.ufbx_load_file(filename, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public static Scene LoadFile(ReadOnlySpan<byte> pathUtf8, LoadOpts options)
    {
        ufbx_error error = default;
        fixed (byte* pathUtf8Ptr = pathUtf8)
        {
        var value = Api.ufbx_load_file_len((sbyte*)pathUtf8Ptr, (nuint)pathUtf8.Length, options.GetUnsafePtr(), &error);
        if (value == null)
        {
            throw new InvalidOperationException(NativeWrapperHelpers.GetString(error.description));
        }
        return new(value);
        }
    }

    public static Scene LoadStdio(void* file, LoadOpts opts, Error error)
    {
        return new(Api.ufbx_load_stdio(file, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public static Scene LoadStdioPrefix(void* file, void* prefix, nuint prefixSize, LoadOpts opts, Error error)
    {
        return new(Api.ufbx_load_stdio_prefix(file, prefix, prefixSize, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public void FreeScene()
    {
        Api.ufbx_free_scene(_ptr);
    }

    public void RetainScene()
    {
        Api.ufbx_retain_scene(_ptr);
    }

    public Element FindElement(ufbx_element_type type, ReadOnlySpan<byte> name)
    {
        fixed (byte* namePtr = name)
        {
        var value = Api.ufbx_find_element_len(_ptr, type, (sbyte*)namePtr, (nuint)name.Length);
        return new(value);
        }
    }

    public Element FindElement(ufbx_element_type type, sbyte* name)
    {
        return new(Api.ufbx_find_element(_ptr, type, name));
    }

    public Node FindNode(ReadOnlySpan<byte> name)
    {
        fixed (byte* namePtr = name)
        {
        var value = Api.ufbx_find_node_len(_ptr, (sbyte*)namePtr, (nuint)name.Length);
        return new(value);
        }
    }

    public Node FindNode(sbyte* name)
    {
        return new(Api.ufbx_find_node(_ptr, name));
    }

    public AnimStack FindAnimStack(ReadOnlySpan<byte> name)
    {
        fixed (byte* namePtr = name)
        {
        var value = Api.ufbx_find_anim_stack_len(_ptr, (sbyte*)namePtr, (nuint)name.Length);
        return new(value);
        }
    }

    public AnimStack FindAnimStack(sbyte* name)
    {
        return new(Api.ufbx_find_anim_stack(_ptr, name));
    }

    public Material FindMaterial(ReadOnlySpan<byte> name)
    {
        fixed (byte* namePtr = name)
        {
        var value = Api.ufbx_find_material_len(_ptr, (sbyte*)namePtr, (nuint)name.Length);
        return new(value);
        }
    }

    public Material FindMaterial(sbyte* name)
    {
        return new(Api.ufbx_find_material(_ptr, name));
    }

    public Scene EvaluateScene(Anim anim, double time, EvaluateOpts opts, Error error)
    {
        return new(Api.ufbx_evaluate_scene(_ptr, anim.GetUnsafePtr(), time, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public Anim CreateAnim(AnimOpts opts, Error error)
    {
        return new(Api.ufbx_create_anim(_ptr, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public BakedAnim BakeAnim(Anim anim, BakeOpts opts, Error error)
    {
        return new(Api.ufbx_bake_anim(_ptr, anim.GetUnsafePtr(), opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public Metadata Metadata => new((ufbx_metadata*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->metadata));

    public SceneSettings Settings => new((ufbx_scene_settings*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->settings));

    public bool HasRootNode => _ptr->root_node != null;
    public Node RootNode => _ptr->root_node != null ? new(_ptr->root_node) : throw new InvalidOperationException("RootNode is null.");

    public bool HasAnim => _ptr->anim != null;
    public Anim Anim => _ptr->anim != null ? new(_ptr->anim) : throw new InvalidOperationException("Anim is null.");

    public ReadOnlySpan<ufbx_texture_file> TextureFiles => _ptr->texture_files.data == null ? ReadOnlySpan<ufbx_texture_file>.Empty : new ReadOnlySpan<ufbx_texture_file>(_ptr->texture_files.data, checked((int)_ptr->texture_files.count));

    public ElementList Elements => new(_ptr->elements.data, _ptr->elements.count);

    public ReadOnlySpan<ufbx_connection> ConnectionsSrc => _ptr->connections_src.data == null ? ReadOnlySpan<ufbx_connection>.Empty : new ReadOnlySpan<ufbx_connection>(_ptr->connections_src.data, checked((int)_ptr->connections_src.count));

    public ReadOnlySpan<ufbx_connection> ConnectionsDst => _ptr->connections_dst.data == null ? ReadOnlySpan<ufbx_connection>.Empty : new ReadOnlySpan<ufbx_connection>(_ptr->connections_dst.data, checked((int)_ptr->connections_dst.count));

    public ReadOnlySpan<ufbx_name_element> ElementsByName => _ptr->elements_by_name.data == null ? ReadOnlySpan<ufbx_name_element>.Empty : new ReadOnlySpan<ufbx_name_element>(_ptr->elements_by_name.data, checked((int)_ptr->elements_by_name.count));

    public bool HasDomRoot => _ptr->dom_root != null;
    public DomNode DomRoot => _ptr->dom_root != null ? new(_ptr->dom_root) : throw new InvalidOperationException("DomRoot is null.");

    public UnknownList Unknowns => new(_ptr->unknowns.data, _ptr->unknowns.count);

    public NodeList Nodes => new(_ptr->nodes.data, _ptr->nodes.count);

    public MeshList Meshes => new(_ptr->meshes.data, _ptr->meshes.count);

    public LightList Lights => new(_ptr->lights.data, _ptr->lights.count);

    public CameraList Cameras => new(_ptr->cameras.data, _ptr->cameras.count);

    public BoneList Bones => new(_ptr->bones.data, _ptr->bones.count);

    public EmptyList Empties => new(_ptr->empties.data, _ptr->empties.count);

    public LineCurveList LineCurves => new(_ptr->line_curves.data, _ptr->line_curves.count);

    public NurbsCurveList NurbsCurves => new(_ptr->nurbs_curves.data, _ptr->nurbs_curves.count);

    public NurbsSurfaceList NurbsSurfaces => new(_ptr->nurbs_surfaces.data, _ptr->nurbs_surfaces.count);

    public NurbsTrimSurfaceList NurbsTrimSurfaces => new(_ptr->nurbs_trim_surfaces.data, _ptr->nurbs_trim_surfaces.count);

    public NurbsTrimBoundaryList NurbsTrimBoundaries => new(_ptr->nurbs_trim_boundaries.data, _ptr->nurbs_trim_boundaries.count);

    public ProceduralGeometryList ProceduralGeometries => new(_ptr->procedural_geometries.data, _ptr->procedural_geometries.count);

    public StereoCameraList StereoCameras => new(_ptr->stereo_cameras.data, _ptr->stereo_cameras.count);

    public CameraSwitcherList CameraSwitchers => new(_ptr->camera_switchers.data, _ptr->camera_switchers.count);

    public MarkerList Markers => new(_ptr->markers.data, _ptr->markers.count);

    public LodGroupList LodGroups => new(_ptr->lod_groups.data, _ptr->lod_groups.count);

    public SkinDeformerList SkinDeformers => new(_ptr->skin_deformers.data, _ptr->skin_deformers.count);

    public SkinClusterList SkinClusters => new(_ptr->skin_clusters.data, _ptr->skin_clusters.count);

    public BlendDeformerList BlendDeformers => new(_ptr->blend_deformers.data, _ptr->blend_deformers.count);

    public BlendChannelList BlendChannels => new(_ptr->blend_channels.data, _ptr->blend_channels.count);

    public BlendShapeList BlendShapes => new(_ptr->blend_shapes.data, _ptr->blend_shapes.count);

    public CacheDeformerList CacheDeformers => new(_ptr->cache_deformers.data, _ptr->cache_deformers.count);

    public CacheFileList CacheFiles => new(_ptr->cache_files.data, _ptr->cache_files.count);

    public MaterialList Materials => new(_ptr->materials.data, _ptr->materials.count);

    public TextureList Textures => new(_ptr->textures.data, _ptr->textures.count);

    public VideoList Videos => new(_ptr->videos.data, _ptr->videos.count);

    public ShaderList Shaders => new(_ptr->shaders.data, _ptr->shaders.count);

    public ShaderBindingList ShaderBindings => new(_ptr->shader_bindings.data, _ptr->shader_bindings.count);

    public AnimStackList AnimStacks => new(_ptr->anim_stacks.data, _ptr->anim_stacks.count);

    public AnimLayerList AnimLayers => new(_ptr->anim_layers.data, _ptr->anim_layers.count);

    public AnimValueList AnimValues => new(_ptr->anim_values.data, _ptr->anim_values.count);

    public AnimCurveList AnimCurves => new(_ptr->anim_curves.data, _ptr->anim_curves.count);

    public DisplayLayerList DisplayLayers => new(_ptr->display_layers.data, _ptr->display_layers.count);

    public SelectionSetList SelectionSets => new(_ptr->selection_sets.data, _ptr->selection_sets.count);

    public SelectionNodeList SelectionNodes => new(_ptr->selection_nodes.data, _ptr->selection_nodes.count);

    public CharacterList Characters => new(_ptr->characters.data, _ptr->characters.count);

    public ConstraintList Constraints => new(_ptr->constraints.data, _ptr->constraints.count);

    public AudioLayerList AudioLayers => new(_ptr->audio_layers.data, _ptr->audio_layers.count);

    public AudioClipList AudioClips => new(_ptr->audio_clips.data, _ptr->audio_clips.count);

    public PoseList Poses => new(_ptr->poses.data, _ptr->poses.count);

    public MetadataObjectList MetadataObjects => new(_ptr->metadata_objects.data, _ptr->metadata_objects.count);

    internal ufbx_scene* GetUnsafePtr() => _ptr;
}
