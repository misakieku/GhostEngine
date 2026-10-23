namespace Ghost.DXC;

/// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind"]/*' />
public enum DxcCursorKind
{
    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnexposedDecl"]/*' />
    DxcCursor_UnexposedDecl = 1,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_StructDecl"]/*' />
    DxcCursor_StructDecl = 2,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnionDecl"]/*' />
    DxcCursor_UnionDecl = 3,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ClassDecl"]/*' />
    DxcCursor_ClassDecl = 4,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_EnumDecl"]/*' />
    DxcCursor_EnumDecl = 5,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FieldDecl"]/*' />
    DxcCursor_FieldDecl = 6,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_EnumConstantDecl"]/*' />
    DxcCursor_EnumConstantDecl = 7,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FunctionDecl"]/*' />
    DxcCursor_FunctionDecl = 8,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_VarDecl"]/*' />
    DxcCursor_VarDecl = 9,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ParmDecl"]/*' />
    DxcCursor_ParmDecl = 10,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCInterfaceDecl"]/*' />
    DxcCursor_ObjCInterfaceDecl = 11,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCCategoryDecl"]/*' />
    DxcCursor_ObjCCategoryDecl = 12,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCProtocolDecl"]/*' />
    DxcCursor_ObjCProtocolDecl = 13,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCPropertyDecl"]/*' />
    DxcCursor_ObjCPropertyDecl = 14,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCIvarDecl"]/*' />
    DxcCursor_ObjCIvarDecl = 15,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCInstanceMethodDecl"]/*' />
    DxcCursor_ObjCInstanceMethodDecl = 16,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCClassMethodDecl"]/*' />
    DxcCursor_ObjCClassMethodDecl = 17,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCImplementationDecl"]/*' />
    DxcCursor_ObjCImplementationDecl = 18,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCCategoryImplDecl"]/*' />
    DxcCursor_ObjCCategoryImplDecl = 19,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TypedefDecl"]/*' />
    DxcCursor_TypedefDecl = 20,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXMethod"]/*' />
    DxcCursor_CXXMethod = 21,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_Namespace"]/*' />
    DxcCursor_Namespace = 22,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LinkageSpec"]/*' />
    DxcCursor_LinkageSpec = 23,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_Constructor"]/*' />
    DxcCursor_Constructor = 24,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_Destructor"]/*' />
    DxcCursor_Destructor = 25,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ConversionFunction"]/*' />
    DxcCursor_ConversionFunction = 26,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TemplateTypeParameter"]/*' />
    DxcCursor_TemplateTypeParameter = 27,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NonTypeTemplateParameter"]/*' />
    DxcCursor_NonTypeTemplateParameter = 28,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TemplateTemplateParameter"]/*' />
    DxcCursor_TemplateTemplateParameter = 29,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FunctionTemplate"]/*' />
    DxcCursor_FunctionTemplate = 30,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ClassTemplate"]/*' />
    DxcCursor_ClassTemplate = 31,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ClassTemplatePartialSpecialization"]/*' />
    DxcCursor_ClassTemplatePartialSpecialization = 32,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NamespaceAlias"]/*' />
    DxcCursor_NamespaceAlias = 33,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UsingDirective"]/*' />
    DxcCursor_UsingDirective = 34,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UsingDeclaration"]/*' />
    DxcCursor_UsingDeclaration = 35,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TypeAliasDecl"]/*' />
    DxcCursor_TypeAliasDecl = 36,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCSynthesizeDecl"]/*' />
    DxcCursor_ObjCSynthesizeDecl = 37,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCDynamicDecl"]/*' />
    DxcCursor_ObjCDynamicDecl = 38,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXAccessSpecifier"]/*' />
    DxcCursor_CXXAccessSpecifier = 39,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstDecl"]/*' />
    DxcCursor_FirstDecl = DxcCursor_UnexposedDecl,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastDecl"]/*' />
    DxcCursor_LastDecl = DxcCursor_CXXAccessSpecifier,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstRef"]/*' />
    DxcCursor_FirstRef = 40,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCSuperClassRef"]/*' />
    DxcCursor_ObjCSuperClassRef = 40,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCProtocolRef"]/*' />
    DxcCursor_ObjCProtocolRef = 41,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCClassRef"]/*' />
    DxcCursor_ObjCClassRef = 42,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TypeRef"]/*' />
    DxcCursor_TypeRef = 43,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXBaseSpecifier"]/*' />
    DxcCursor_CXXBaseSpecifier = 44,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TemplateRef"]/*' />
    DxcCursor_TemplateRef = 45,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NamespaceRef"]/*' />
    DxcCursor_NamespaceRef = 46,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_MemberRef"]/*' />
    DxcCursor_MemberRef = 47,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LabelRef"]/*' />
    DxcCursor_LabelRef = 48,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OverloadedDeclRef"]/*' />
    DxcCursor_OverloadedDeclRef = 49,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_VariableRef"]/*' />
    DxcCursor_VariableRef = 50,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastRef"]/*' />
    DxcCursor_LastRef = DxcCursor_VariableRef,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstInvalid"]/*' />
    DxcCursor_FirstInvalid = 70,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_InvalidFile"]/*' />
    DxcCursor_InvalidFile = 70,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NoDeclFound"]/*' />
    DxcCursor_NoDeclFound = 71,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NotImplemented"]/*' />
    DxcCursor_NotImplemented = 72,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_InvalidCode"]/*' />
    DxcCursor_InvalidCode = 73,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastInvalid"]/*' />
    DxcCursor_LastInvalid = DxcCursor_InvalidCode,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstExpr"]/*' />
    DxcCursor_FirstExpr = 100,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnexposedExpr"]/*' />
    DxcCursor_UnexposedExpr = 100,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_DeclRefExpr"]/*' />
    DxcCursor_DeclRefExpr = 101,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_MemberRefExpr"]/*' />
    DxcCursor_MemberRefExpr = 102,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CallExpr"]/*' />
    DxcCursor_CallExpr = 103,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCMessageExpr"]/*' />
    DxcCursor_ObjCMessageExpr = 104,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_BlockExpr"]/*' />
    DxcCursor_BlockExpr = 105,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_IntegerLiteral"]/*' />
    DxcCursor_IntegerLiteral = 106,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FloatingLiteral"]/*' />
    DxcCursor_FloatingLiteral = 107,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ImaginaryLiteral"]/*' />
    DxcCursor_ImaginaryLiteral = 108,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_StringLiteral"]/*' />
    DxcCursor_StringLiteral = 109,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CharacterLiteral"]/*' />
    DxcCursor_CharacterLiteral = 110,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ParenExpr"]/*' />
    DxcCursor_ParenExpr = 111,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnaryOperator"]/*' />
    DxcCursor_UnaryOperator = 112,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ArraySubscriptExpr"]/*' />
    DxcCursor_ArraySubscriptExpr = 113,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_BinaryOperator"]/*' />
    DxcCursor_BinaryOperator = 114,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CompoundAssignOperator"]/*' />
    DxcCursor_CompoundAssignOperator = 115,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ConditionalOperator"]/*' />
    DxcCursor_ConditionalOperator = 116,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CStyleCastExpr"]/*' />
    DxcCursor_CStyleCastExpr = 117,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CompoundLiteralExpr"]/*' />
    DxcCursor_CompoundLiteralExpr = 118,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_InitListExpr"]/*' />
    DxcCursor_InitListExpr = 119,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_AddrLabelExpr"]/*' />
    DxcCursor_AddrLabelExpr = 120,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_StmtExpr"]/*' />
    DxcCursor_StmtExpr = 121,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_GenericSelectionExpr"]/*' />
    DxcCursor_GenericSelectionExpr = 122,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_GNUNullExpr"]/*' />
    DxcCursor_GNUNullExpr = 123,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXStaticCastExpr"]/*' />
    DxcCursor_CXXStaticCastExpr = 124,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXDynamicCastExpr"]/*' />
    DxcCursor_CXXDynamicCastExpr = 125,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXReinterpretCastExpr"]/*' />
    DxcCursor_CXXReinterpretCastExpr = 126,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXConstCastExpr"]/*' />
    DxcCursor_CXXConstCastExpr = 127,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXFunctionalCastExpr"]/*' />
    DxcCursor_CXXFunctionalCastExpr = 128,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXTypeidExpr"]/*' />
    DxcCursor_CXXTypeidExpr = 129,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXBoolLiteralExpr"]/*' />
    DxcCursor_CXXBoolLiteralExpr = 130,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXNullPtrLiteralExpr"]/*' />
    DxcCursor_CXXNullPtrLiteralExpr = 131,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXThisExpr"]/*' />
    DxcCursor_CXXThisExpr = 132,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXThrowExpr"]/*' />
    DxcCursor_CXXThrowExpr = 133,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXNewExpr"]/*' />
    DxcCursor_CXXNewExpr = 134,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXDeleteExpr"]/*' />
    DxcCursor_CXXDeleteExpr = 135,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnaryExpr"]/*' />
    DxcCursor_UnaryExpr = 136,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCStringLiteral"]/*' />
    DxcCursor_ObjCStringLiteral = 137,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCEncodeExpr"]/*' />
    DxcCursor_ObjCEncodeExpr = 138,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCSelectorExpr"]/*' />
    DxcCursor_ObjCSelectorExpr = 139,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCProtocolExpr"]/*' />
    DxcCursor_ObjCProtocolExpr = 140,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCBridgedCastExpr"]/*' />
    DxcCursor_ObjCBridgedCastExpr = 141,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_PackExpansionExpr"]/*' />
    DxcCursor_PackExpansionExpr = 142,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_SizeOfPackExpr"]/*' />
    DxcCursor_SizeOfPackExpr = 143,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LambdaExpr"]/*' />
    DxcCursor_LambdaExpr = 144,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCBoolLiteralExpr"]/*' />
    DxcCursor_ObjCBoolLiteralExpr = 145,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCSelfExpr"]/*' />
    DxcCursor_ObjCSelfExpr = 146,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastExpr"]/*' />
    DxcCursor_LastExpr = DxcCursor_ObjCSelfExpr,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstStmt"]/*' />
    DxcCursor_FirstStmt = 200,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnexposedStmt"]/*' />
    DxcCursor_UnexposedStmt = 200,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LabelStmt"]/*' />
    DxcCursor_LabelStmt = 201,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CompoundStmt"]/*' />
    DxcCursor_CompoundStmt = 202,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CaseStmt"]/*' />
    DxcCursor_CaseStmt = 203,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_DefaultStmt"]/*' />
    DxcCursor_DefaultStmt = 204,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_IfStmt"]/*' />
    DxcCursor_IfStmt = 205,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_SwitchStmt"]/*' />
    DxcCursor_SwitchStmt = 206,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_WhileStmt"]/*' />
    DxcCursor_WhileStmt = 207,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_DoStmt"]/*' />
    DxcCursor_DoStmt = 208,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ForStmt"]/*' />
    DxcCursor_ForStmt = 209,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_GotoStmt"]/*' />
    DxcCursor_GotoStmt = 210,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_IndirectGotoStmt"]/*' />
    DxcCursor_IndirectGotoStmt = 211,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ContinueStmt"]/*' />
    DxcCursor_ContinueStmt = 212,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_BreakStmt"]/*' />
    DxcCursor_BreakStmt = 213,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ReturnStmt"]/*' />
    DxcCursor_ReturnStmt = 214,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_GCCAsmStmt"]/*' />
    DxcCursor_GCCAsmStmt = 215,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_AsmStmt"]/*' />
    DxcCursor_AsmStmt = DxcCursor_GCCAsmStmt,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCAtTryStmt"]/*' />
    DxcCursor_ObjCAtTryStmt = 216,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCAtCatchStmt"]/*' />
    DxcCursor_ObjCAtCatchStmt = 217,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCAtFinallyStmt"]/*' />
    DxcCursor_ObjCAtFinallyStmt = 218,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCAtThrowStmt"]/*' />
    DxcCursor_ObjCAtThrowStmt = 219,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCAtSynchronizedStmt"]/*' />
    DxcCursor_ObjCAtSynchronizedStmt = 220,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCAutoreleasePoolStmt"]/*' />
    DxcCursor_ObjCAutoreleasePoolStmt = 221,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ObjCForCollectionStmt"]/*' />
    DxcCursor_ObjCForCollectionStmt = 222,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXCatchStmt"]/*' />
    DxcCursor_CXXCatchStmt = 223,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXTryStmt"]/*' />
    DxcCursor_CXXTryStmt = 224,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXForRangeStmt"]/*' />
    DxcCursor_CXXForRangeStmt = 225,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_SEHTryStmt"]/*' />
    DxcCursor_SEHTryStmt = 226,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_SEHExceptStmt"]/*' />
    DxcCursor_SEHExceptStmt = 227,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_SEHFinallyStmt"]/*' />
    DxcCursor_SEHFinallyStmt = 228,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_MSAsmStmt"]/*' />
    DxcCursor_MSAsmStmt = 229,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NullStmt"]/*' />
    DxcCursor_NullStmt = 230,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_DeclStmt"]/*' />
    DxcCursor_DeclStmt = 231,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPParallelDirective"]/*' />
    DxcCursor_OMPParallelDirective = 232,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPSimdDirective"]/*' />
    DxcCursor_OMPSimdDirective = 233,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPForDirective"]/*' />
    DxcCursor_OMPForDirective = 234,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPSectionsDirective"]/*' />
    DxcCursor_OMPSectionsDirective = 235,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPSectionDirective"]/*' />
    DxcCursor_OMPSectionDirective = 236,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPSingleDirective"]/*' />
    DxcCursor_OMPSingleDirective = 237,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPParallelForDirective"]/*' />
    DxcCursor_OMPParallelForDirective = 238,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPParallelSectionsDirective"]/*' />
    DxcCursor_OMPParallelSectionsDirective = 239,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPTaskDirective"]/*' />
    DxcCursor_OMPTaskDirective = 240,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPMasterDirective"]/*' />
    DxcCursor_OMPMasterDirective = 241,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPCriticalDirective"]/*' />
    DxcCursor_OMPCriticalDirective = 242,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPTaskyieldDirective"]/*' />
    DxcCursor_OMPTaskyieldDirective = 243,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPBarrierDirective"]/*' />
    DxcCursor_OMPBarrierDirective = 244,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPTaskwaitDirective"]/*' />
    DxcCursor_OMPTaskwaitDirective = 245,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPFlushDirective"]/*' />
    DxcCursor_OMPFlushDirective = 246,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_SEHLeaveStmt"]/*' />
    DxcCursor_SEHLeaveStmt = 247,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPOrderedDirective"]/*' />
    DxcCursor_OMPOrderedDirective = 248,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPAtomicDirective"]/*' />
    DxcCursor_OMPAtomicDirective = 249,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPForSimdDirective"]/*' />
    DxcCursor_OMPForSimdDirective = 250,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPParallelForSimdDirective"]/*' />
    DxcCursor_OMPParallelForSimdDirective = 251,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPTargetDirective"]/*' />
    DxcCursor_OMPTargetDirective = 252,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPTeamsDirective"]/*' />
    DxcCursor_OMPTeamsDirective = 253,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPTaskgroupDirective"]/*' />
    DxcCursor_OMPTaskgroupDirective = 254,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPCancellationPointDirective"]/*' />
    DxcCursor_OMPCancellationPointDirective = 255,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_OMPCancelDirective"]/*' />
    DxcCursor_OMPCancelDirective = 256,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastStmt"]/*' />
    DxcCursor_LastStmt = DxcCursor_OMPCancelDirective,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_TranslationUnit"]/*' />
    DxcCursor_TranslationUnit = 300,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstAttr"]/*' />
    DxcCursor_FirstAttr = 400,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_UnexposedAttr"]/*' />
    DxcCursor_UnexposedAttr = 400,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_IBActionAttr"]/*' />
    DxcCursor_IBActionAttr = 401,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_IBOutletAttr"]/*' />
    DxcCursor_IBOutletAttr = 402,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_IBOutletCollectionAttr"]/*' />
    DxcCursor_IBOutletCollectionAttr = 403,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXFinalAttr"]/*' />
    DxcCursor_CXXFinalAttr = 404,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CXXOverrideAttr"]/*' />
    DxcCursor_CXXOverrideAttr = 405,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_AnnotateAttr"]/*' />
    DxcCursor_AnnotateAttr = 406,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_AsmLabelAttr"]/*' />
    DxcCursor_AsmLabelAttr = 407,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_PackedAttr"]/*' />
    DxcCursor_PackedAttr = 408,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_PureAttr"]/*' />
    DxcCursor_PureAttr = 409,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ConstAttr"]/*' />
    DxcCursor_ConstAttr = 410,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_NoDuplicateAttr"]/*' />
    DxcCursor_NoDuplicateAttr = 411,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CUDAConstantAttr"]/*' />
    DxcCursor_CUDAConstantAttr = 412,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CUDADeviceAttr"]/*' />
    DxcCursor_CUDADeviceAttr = 413,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CUDAGlobalAttr"]/*' />
    DxcCursor_CUDAGlobalAttr = 414,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CUDAHostAttr"]/*' />
    DxcCursor_CUDAHostAttr = 415,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_CUDASharedAttr"]/*' />
    DxcCursor_CUDASharedAttr = 416,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastAttr"]/*' />
    DxcCursor_LastAttr = DxcCursor_CUDASharedAttr,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_PreprocessingDirective"]/*' />
    DxcCursor_PreprocessingDirective = 500,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_MacroDefinition"]/*' />
    DxcCursor_MacroDefinition = 501,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_MacroExpansion"]/*' />
    DxcCursor_MacroExpansion = 502,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_MacroInstantiation"]/*' />
    DxcCursor_MacroInstantiation = DxcCursor_MacroExpansion,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_InclusionDirective"]/*' />
    DxcCursor_InclusionDirective = 503,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstPreprocessing"]/*' />
    DxcCursor_FirstPreprocessing = DxcCursor_PreprocessingDirective,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastPreprocessing"]/*' />
    DxcCursor_LastPreprocessing = DxcCursor_InclusionDirective,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_ModuleImportDecl"]/*' />
    DxcCursor_ModuleImportDecl = 600,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_FirstExtraDecl"]/*' />
    DxcCursor_FirstExtraDecl = DxcCursor_ModuleImportDecl,

    /// <include file='DxcCursorKind.xml' path='doc/member[@name="DxcCursorKind.DxcCursor_LastExtraDecl"]/*' />
    DxcCursor_LastExtraDecl = DxcCursor_ModuleImportDecl,
}
