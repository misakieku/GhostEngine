namespace Ghost.DXC;

/// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind"]/*' />
public enum DxcTypeKind
{
    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Invalid"]/*' />
    DxcTypeKind_Invalid = 0,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Unexposed"]/*' />
    DxcTypeKind_Unexposed = 1,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Void"]/*' />
    DxcTypeKind_Void = 2,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Bool"]/*' />
    DxcTypeKind_Bool = 3,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Char_U"]/*' />
    DxcTypeKind_Char_U = 4,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_UChar"]/*' />
    DxcTypeKind_UChar = 5,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Char16"]/*' />
    DxcTypeKind_Char16 = 6,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Char32"]/*' />
    DxcTypeKind_Char32 = 7,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_UShort"]/*' />
    DxcTypeKind_UShort = 8,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_UInt"]/*' />
    DxcTypeKind_UInt = 9,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ULong"]/*' />
    DxcTypeKind_ULong = 10,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ULongLong"]/*' />
    DxcTypeKind_ULongLong = 11,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_UInt128"]/*' />
    DxcTypeKind_UInt128 = 12,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Char_S"]/*' />
    DxcTypeKind_Char_S = 13,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_SChar"]/*' />
    DxcTypeKind_SChar = 14,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_WChar"]/*' />
    DxcTypeKind_WChar = 15,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Short"]/*' />
    DxcTypeKind_Short = 16,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Int"]/*' />
    DxcTypeKind_Int = 17,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Long"]/*' />
    DxcTypeKind_Long = 18,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_LongLong"]/*' />
    DxcTypeKind_LongLong = 19,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Int128"]/*' />
    DxcTypeKind_Int128 = 20,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Float"]/*' />
    DxcTypeKind_Float = 21,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Double"]/*' />
    DxcTypeKind_Double = 22,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_LongDouble"]/*' />
    DxcTypeKind_LongDouble = 23,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_NullPtr"]/*' />
    DxcTypeKind_NullPtr = 24,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Overload"]/*' />
    DxcTypeKind_Overload = 25,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Dependent"]/*' />
    DxcTypeKind_Dependent = 26,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ObjCId"]/*' />
    DxcTypeKind_ObjCId = 27,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ObjCClass"]/*' />
    DxcTypeKind_ObjCClass = 28,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ObjCSel"]/*' />
    DxcTypeKind_ObjCSel = 29,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_FirstBuiltin"]/*' />
    DxcTypeKind_FirstBuiltin = DxcTypeKind_Void,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_LastBuiltin"]/*' />
    DxcTypeKind_LastBuiltin = DxcTypeKind_ObjCSel,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Complex"]/*' />
    DxcTypeKind_Complex = 100,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Pointer"]/*' />
    DxcTypeKind_Pointer = 101,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_BlockPointer"]/*' />
    DxcTypeKind_BlockPointer = 102,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_LValueReference"]/*' />
    DxcTypeKind_LValueReference = 103,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_RValueReference"]/*' />
    DxcTypeKind_RValueReference = 104,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Record"]/*' />
    DxcTypeKind_Record = 105,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Enum"]/*' />
    DxcTypeKind_Enum = 106,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Typedef"]/*' />
    DxcTypeKind_Typedef = 107,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ObjCInterface"]/*' />
    DxcTypeKind_ObjCInterface = 108,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ObjCObjectPointer"]/*' />
    DxcTypeKind_ObjCObjectPointer = 109,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_FunctionNoProto"]/*' />
    DxcTypeKind_FunctionNoProto = 110,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_FunctionProto"]/*' />
    DxcTypeKind_FunctionProto = 111,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_ConstantArray"]/*' />
    DxcTypeKind_ConstantArray = 112,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_Vector"]/*' />
    DxcTypeKind_Vector = 113,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_IncompleteArray"]/*' />
    DxcTypeKind_IncompleteArray = 114,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_VariableArray"]/*' />
    DxcTypeKind_VariableArray = 115,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_DependentSizedArray"]/*' />
    DxcTypeKind_DependentSizedArray = 116,

    /// <include file='DxcTypeKind.xml' path='doc/member[@name="DxcTypeKind.DxcTypeKind_MemberPointer"]/*' />
    DxcTypeKind_MemberPointer = 117,
}
