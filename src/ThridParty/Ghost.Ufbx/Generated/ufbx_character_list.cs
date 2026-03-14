namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_character_list
    {
        public ufbx_character** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
