namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_face_group_list
    {
        public ufbx_face_group* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
