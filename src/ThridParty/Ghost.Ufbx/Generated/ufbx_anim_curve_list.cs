namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_curve_list
    {
        public ufbx_anim_curve** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
