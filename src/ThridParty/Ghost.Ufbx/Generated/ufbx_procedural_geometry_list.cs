namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_procedural_geometry_list
    {
        public ufbx_procedural_geometry** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
