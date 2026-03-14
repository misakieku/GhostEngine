namespace Ghost.Ufbx;

public unsafe struct MeshPart
{
    private ufbx_mesh_part* _ptr;

    internal MeshPart(ufbx_mesh_part* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint Index => _ptr->index;

    public nuint NumFaces => _ptr->num_faces;

    public nuint NumTriangles => _ptr->num_triangles;

    public nuint NumEmptyFaces => _ptr->num_empty_faces;

    public nuint NumPointFaces => _ptr->num_point_faces;

    public nuint NumLineFaces => _ptr->num_line_faces;

    public ReadOnlySpan<uint> FaceIndices => _ptr->face_indices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->face_indices.data, checked((int)_ptr->face_indices.count));

    internal ufbx_mesh_part* GetUnsafePtr() => _ptr;
}
