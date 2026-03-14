namespace Ghost.Ufbx;

public unsafe struct SkinCluster
{
    private ufbx_skin_cluster* _ptr;

    internal SkinCluster(ufbx_skin_cluster* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasBoneNode => _ptr->bone_node != null;
    public Node BoneNode => _ptr->bone_node != null ? new(_ptr->bone_node) : throw new InvalidOperationException("BoneNode is null.");

    public Misaki.HighPerformance.Mathematics.float3x4 GeometryToBone => _ptr->geometry_to_bone;

    public Misaki.HighPerformance.Mathematics.float3x4 MeshNodeToBone => _ptr->mesh_node_to_bone;

    public Misaki.HighPerformance.Mathematics.float3x4 BindToWorld => _ptr->bind_to_world;

    public Misaki.HighPerformance.Mathematics.float3x4 GeometryToWorld => _ptr->geometry_to_world;

    public Transform GeometryToWorldTransform => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->geometry_to_world_transform));

    public nuint NumWeights => _ptr->num_weights;

    public ReadOnlySpan<uint> Vertices => _ptr->vertices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->vertices.data, checked((int)_ptr->vertices.count));

    public ReadOnlySpan<float> Weights => _ptr->weights.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->weights.data, checked((int)_ptr->weights.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_skin_cluster* GetUnsafePtr() => _ptr;
}
