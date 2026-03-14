using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_cache_file
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2246_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_string filename;

        public ufbx_string absolute_filename;

        public ufbx_string relative_filename;

        public ufbx_blob raw_filename;

        public ufbx_blob raw_absolute_filename;

        public ufbx_blob raw_relative_filename;

        public ufbx_cache_file_format format;

        public ufbx_geometry_cache* external_cache;

        [UnscopedRef]
        public ref ufbx_element element
        {
            get
            {
                return ref Anonymous.element;
            }
        }

        [UnscopedRef]
        public ref ufbx_string name
        {
            get
            {
                return ref Anonymous.Anonymous.name;
            }
        }

        [UnscopedRef]
        public ref ufbx_props props
        {
            get
            {
                return ref Anonymous.Anonymous.props;
            }
        }

        [UnscopedRef]
        public ref uint element_id
        {
            get
            {
                return ref Anonymous.Anonymous.element_id;
            }
        }

        [UnscopedRef]
        public ref uint typed_id
        {
            get
            {
                return ref Anonymous.Anonymous.typed_id;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            public ufbx_element element;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2246_C32")]
            public _Anonymous_e__Struct Anonymous;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_string name;

                public ufbx_props props;

                [NativeTypeName("uint32_t")]
                public uint element_id;

                [NativeTypeName("uint32_t")]
                public uint typed_id;
            }
        }
    }
}
