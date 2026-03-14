namespace Ghost.Ufbx;

public unsafe ref struct Node
{
    private ufbx_node* _ptr;

    internal Node(ufbx_node* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Misaki.HighPerformance.Mathematics.float3x4 GetCompatibleMatrixForNormals()
    {
        return Api.ufbx_get_compatible_matrix_for_normals(_ptr);
    }

    public bool HasParent => _ptr->parent != null;
    public Node Parent => _ptr->parent != null ? new(_ptr->parent) : throw new InvalidOperationException("Parent is null.");

    public NodeList Children => new(_ptr->children.data, _ptr->children.count);

    public bool HasMesh => _ptr->mesh != null;
    public Mesh Mesh => _ptr->mesh != null ? new(_ptr->mesh) : throw new InvalidOperationException("Mesh is null.");

    public bool HasLight => _ptr->light != null;
    public Light Light => _ptr->light != null ? new(_ptr->light) : throw new InvalidOperationException("Light is null.");

    public bool HasCamera => _ptr->camera != null;
    public Camera Camera => _ptr->camera != null ? new(_ptr->camera) : throw new InvalidOperationException("Camera is null.");

    public bool HasBone => _ptr->bone != null;
    public Bone Bone => _ptr->bone != null ? new(_ptr->bone) : throw new InvalidOperationException("Bone is null.");

    public bool HasAttrib => _ptr->attrib != null;
    public Element Attrib => _ptr->attrib != null ? new(_ptr->attrib) : throw new InvalidOperationException("Attrib is null.");

    public bool HasGeometryTransformHelper => _ptr->geometry_transform_helper != null;
    public Node GeometryTransformHelper => _ptr->geometry_transform_helper != null ? new(_ptr->geometry_transform_helper) : throw new InvalidOperationException("GeometryTransformHelper is null.");

    public bool HasScaleHelper => _ptr->scale_helper != null;
    public Node ScaleHelper => _ptr->scale_helper != null ? new(_ptr->scale_helper) : throw new InvalidOperationException("ScaleHelper is null.");

    public ufbx_element_type AttribType => _ptr->attrib_type;

    public ElementList AllAttribs => new(_ptr->all_attribs.data, _ptr->all_attribs.count);

    public ufbx_inherit_mode InheritMode => _ptr->inherit_mode;

    public ufbx_inherit_mode OriginalInheritMode => _ptr->original_inherit_mode;

    public Transform LocalTransform => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->local_transform));

    public Transform GeometryTransform => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->geometry_transform));

    public Misaki.HighPerformance.Mathematics.float3 InheritScale => _ptr->inherit_scale;

    public bool HasInheritScaleNode => _ptr->inherit_scale_node != null;
    public Node InheritScaleNode => _ptr->inherit_scale_node != null ? new(_ptr->inherit_scale_node) : throw new InvalidOperationException("InheritScaleNode is null.");

    public ufbx_rotation_order RotationOrder => _ptr->rotation_order;

    public Misaki.HighPerformance.Mathematics.float3 EulerRotation => _ptr->euler_rotation;

    public Misaki.HighPerformance.Mathematics.float3x4 NodeToParent => _ptr->node_to_parent;

    public Misaki.HighPerformance.Mathematics.float3x4 NodeToWorld => _ptr->node_to_world;

    public Misaki.HighPerformance.Mathematics.float3x4 GeometryToNode => _ptr->geometry_to_node;

    public Misaki.HighPerformance.Mathematics.float3x4 GeometryToWorld => _ptr->geometry_to_world;

    public Misaki.HighPerformance.Mathematics.float3x4 UnscaledNodeToWorld => _ptr->unscaled_node_to_world;

    public Misaki.HighPerformance.Mathematics.float3 AdjustPreTranslation => _ptr->adjust_pre_translation;

    public Misaki.HighPerformance.Mathematics.quaternion AdjustPreRotation => _ptr->adjust_pre_rotation;

    public float AdjustPreScale => _ptr->adjust_pre_scale;

    public Misaki.HighPerformance.Mathematics.quaternion AdjustPostRotation => _ptr->adjust_post_rotation;

    public float AdjustPostScale => _ptr->adjust_post_scale;

    public float AdjustTranslationScale => _ptr->adjust_translation_scale;

    public ufbx_mirror_axis AdjustMirrorAxis => _ptr->adjust_mirror_axis;

    public MaterialList Materials => new(_ptr->materials.data, _ptr->materials.count);

    public bool HasBindPose => _ptr->bind_pose != null;
    public Pose BindPose => _ptr->bind_pose != null ? new(_ptr->bind_pose) : throw new InvalidOperationException("BindPose is null.");

    public bool Visible => _ptr->visible;

    public bool IsRoot => _ptr->is_root;

    public bool HasGeometryTransform => _ptr->has_geometry_transform;

    public bool HasAdjustTransform => _ptr->has_adjust_transform;

    public bool HasRootAdjustTransform => _ptr->has_root_adjust_transform;

    public bool IsGeometryTransformHelper => _ptr->is_geometry_transform_helper;

    public bool IsScaleHelper => _ptr->is_scale_helper;

    public bool IsScaleCompensateParent => _ptr->is_scale_compensate_parent;

    public uint NodeDepth => _ptr->node_depth;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_node* GetUnsafePtr() => _ptr;
}
