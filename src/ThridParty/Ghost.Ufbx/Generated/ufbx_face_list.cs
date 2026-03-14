namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_face_list
    {
        public ufbx_face* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
