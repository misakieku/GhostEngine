using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip"]/*' />
    public partial struct ufbx_audio_clip
    {
        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L3411_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.filename"]/*' />
        public ufbx_string filename;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.absolute_filename"]/*' />
        public ufbx_string absolute_filename;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.relative_filename"]/*' />
        public ufbx_string relative_filename;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.raw_filename"]/*' />
        public ufbx_blob raw_filename;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.raw_absolute_filename"]/*' />
        public ufbx_blob raw_absolute_filename;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.raw_relative_filename"]/*' />
        public ufbx_blob raw_relative_filename;

        /// <include file='ufbx_audio_clip.xml' path='doc/member[@name="ufbx_audio_clip.content"]/*' />
        public ufbx_blob content;

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.element"]/*' />
        [UnscopedRef]
        public ref ufbx_element element
        {
            get
            {
                return ref Anonymous.element;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.name"]/*' />
        [UnscopedRef]
        public ref ufbx_string name
        {
            get
            {
                return ref Anonymous.Anonymous.name;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.props"]/*' />
        [UnscopedRef]
        public ref ufbx_props props
        {
            get
            {
                return ref Anonymous.Anonymous.props;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.element_id"]/*' />
        [UnscopedRef]
        public ref uint element_id
        {
            get
            {
                return ref Anonymous.Anonymous.element_id;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.typed_id"]/*' />
        [UnscopedRef]
        public ref uint typed_id
        {
            get
            {
                return ref Anonymous.Anonymous.typed_id;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.element"]/*' />
            [FieldOffset(0)]
            public ufbx_element element;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L3411_C32")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.name"]/*' />
                public ufbx_string name;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.props"]/*' />
                public ufbx_props props;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.element_id"]/*' />
                [NativeTypeName("uint32_t")]
                public uint element_id;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.typed_id"]/*' />
                [NativeTypeName("uint32_t")]
                public uint typed_id;
            }
        }
    }
}
