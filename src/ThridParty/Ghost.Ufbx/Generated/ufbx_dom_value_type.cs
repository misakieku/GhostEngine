namespace Ghost.Ufbx
{
    public enum ufbx_dom_value_type
    {
        UFBX_DOM_VALUE_NUMBER,
        UFBX_DOM_VALUE_STRING,
        UFBX_DOM_VALUE_BLOB,
        UFBX_DOM_VALUE_ARRAY_I32,
        UFBX_DOM_VALUE_ARRAY_I64,
        UFBX_DOM_VALUE_ARRAY_F32,
        UFBX_DOM_VALUE_ARRAY_F64,
        UFBX_DOM_VALUE_ARRAY_BLOB,
        UFBX_DOM_VALUE_ARRAY_IGNORED,
        UFBX_DOM_VALUE_TYPE_FORCE_32BIT = 0x7fffffff,
    }
}
