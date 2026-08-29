namespace Ghost.Ufbx
{
    /// <include file='ufbx_character_list.xml' path='doc/member[@name="ufbx_character_list"]/*' />
    public unsafe partial struct ufbx_character_list
    {
        /// <include file='ufbx_character_list.xml' path='doc/member[@name="ufbx_character_list.data"]/*' />
        public ufbx_character** data;

        /// <include file='ufbx_character_list.xml' path='doc/member[@name="ufbx_character_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
