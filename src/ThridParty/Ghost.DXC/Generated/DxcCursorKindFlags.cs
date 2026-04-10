namespace Ghost.DXC;

/// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags"]/*' />
public enum DxcCursorKindFlags
{
    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_None"]/*' />
    DxcCursorKind_None = 0,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Declaration"]/*' />
    DxcCursorKind_Declaration = 0x1,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Reference"]/*' />
    DxcCursorKind_Reference = 0x2,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Expression"]/*' />
    DxcCursorKind_Expression = 0x4,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Statement"]/*' />
    DxcCursorKind_Statement = 0x8,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Attribute"]/*' />
    DxcCursorKind_Attribute = 0x10,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Invalid"]/*' />
    DxcCursorKind_Invalid = 0x20,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_TranslationUnit"]/*' />
    DxcCursorKind_TranslationUnit = 0x40,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Preprocessing"]/*' />
    DxcCursorKind_Preprocessing = 0x80,

    /// <include file='DxcCursorKindFlags.xml' path='doc/member[@name="DxcCursorKindFlags.DxcCursorKind_Unexposed"]/*' />
    DxcCursorKind_Unexposed = 0x100,
}
