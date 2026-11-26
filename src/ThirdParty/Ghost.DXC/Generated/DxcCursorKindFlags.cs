namespace Ghost.DXC;

public enum DxcCursorKindFlags
{
    DxcCursorKind_None = 0,
    DxcCursorKind_Declaration = 0x1,
    DxcCursorKind_Reference = 0x2,
    DxcCursorKind_Expression = 0x4,
    DxcCursorKind_Statement = 0x8,
    DxcCursorKind_Attribute = 0x10,
    DxcCursorKind_Invalid = 0x20,
    DxcCursorKind_TranslationUnit = 0x40,
    DxcCursorKind_Preprocessing = 0x80,
    DxcCursorKind_Unexposed = 0x100,
}
