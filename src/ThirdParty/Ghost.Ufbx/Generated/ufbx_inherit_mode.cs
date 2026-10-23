namespace Ghost.Ufbx
{
    /// <include file='ufbx_inherit_mode.xml' path='doc/member[@name="ufbx_inherit_mode"]/*' />
    public enum ufbx_inherit_mode
    {
        /// <include file='ufbx_inherit_mode.xml' path='doc/member[@name="ufbx_inherit_mode.UFBX_INHERIT_MODE_NORMAL"]/*' />
        UFBX_INHERIT_MODE_NORMAL,

        /// <include file='ufbx_inherit_mode.xml' path='doc/member[@name="ufbx_inherit_mode.UFBX_INHERIT_MODE_IGNORE_PARENT_SCALE"]/*' />
        UFBX_INHERIT_MODE_IGNORE_PARENT_SCALE,

        /// <include file='ufbx_inherit_mode.xml' path='doc/member[@name="ufbx_inherit_mode.UFBX_INHERIT_MODE_COMPONENTWISE_SCALE"]/*' />
        UFBX_INHERIT_MODE_COMPONENTWISE_SCALE,

        /// <include file='ufbx_inherit_mode.xml' path='doc/member[@name="ufbx_inherit_mode.UFBX_INHERIT_MODE_FORCE_32BIT"]/*' />
        UFBX_INHERIT_MODE_FORCE_32BIT = 0x7fffffff,
    }
}
