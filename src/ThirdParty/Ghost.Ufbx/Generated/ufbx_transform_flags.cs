namespace Ghost.Ufbx
{
    /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags"]/*' />
    public enum ufbx_transform_flags
    {
        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_IGNORE_SCALE_HELPER"]/*' />
        UFBX_TRANSFORM_FLAG_IGNORE_SCALE_HELPER = 0x1,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_IGNORE_COMPONENTWISE_SCALE"]/*' />
        UFBX_TRANSFORM_FLAG_IGNORE_COMPONENTWISE_SCALE = 0x2,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_EXPLICIT_INCLUDES"]/*' />
        UFBX_TRANSFORM_FLAG_EXPLICIT_INCLUDES = 0x4,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_INCLUDE_TRANSLATION"]/*' />
        UFBX_TRANSFORM_FLAG_INCLUDE_TRANSLATION = 0x10,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_INCLUDE_ROTATION"]/*' />
        UFBX_TRANSFORM_FLAG_INCLUDE_ROTATION = 0x20,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_INCLUDE_SCALE"]/*' />
        UFBX_TRANSFORM_FLAG_INCLUDE_SCALE = 0x40,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAG_NO_EXTRAPOLATION"]/*' />
        UFBX_TRANSFORM_FLAG_NO_EXTRAPOLATION = 0x80,

        /// <include file='ufbx_transform_flags.xml' path='doc/member[@name="ufbx_transform_flags.UFBX_TRANSFORM_FLAGS_FORCE_32BIT"]/*' />
        UFBX_TRANSFORM_FLAGS_FORCE_32BIT = 0x7fffffff,
    }
}
