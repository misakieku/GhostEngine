namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_string
    {
        [NativeTypeName("const char *")]
        public sbyte* data;

        [NativeTypeName("size_t")]
        public nuint length;
    }
}
