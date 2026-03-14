namespace Ghost.Ufbx
{
    public enum ufbx_prop_type
    {
        UFBX_PROP_UNKNOWN,
        UFBX_PROP_BOOLEAN,
        UFBX_PROP_INTEGER,
        UFBX_PROP_NUMBER,
        UFBX_PROP_VECTOR,
        UFBX_PROP_COLOR,
        UFBX_PROP_COLOR_WITH_ALPHA,
        UFBX_PROP_STRING,
        UFBX_PROP_DATE_TIME,
        UFBX_PROP_TRANSLATION,
        UFBX_PROP_ROTATION,
        UFBX_PROP_SCALING,
        UFBX_PROP_DISTANCE,
        UFBX_PROP_COMPOUND,
        UFBX_PROP_BLOB,
        UFBX_PROP_REFERENCE,
        UFBX_PROP_TYPE_FORCE_32BIT = 0x7fffffff,
    }
}
