namespace Ghost.DXC;

public enum DxcTokenKind
{
    DxcTokenKind_Punctuation = 0,
    DxcTokenKind_Keyword = 1,
    DxcTokenKind_Identifier = 2,
    DxcTokenKind_Literal = 3,
    DxcTokenKind_Comment = 4,
    DxcTokenKind_Unknown = 5,
    DxcTokenKind_BuiltInType = 6,
}
