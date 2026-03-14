namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_keyframe_list
    {
        public ufbx_keyframe* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
