using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor"]/*' />
[Guid("1467B985-288D-4D2A-80C1-EF89C42C40BC")]
[NativeTypeName("struct IDxcCursor : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcCursor : IDxcCursor.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcCursor);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, Guid*, void**, int>)(lpVtbl[0]))((IDxcCursor*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, uint>)(lpVtbl[1]))((IDxcCursor*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, uint>)(lpVtbl[2]))((IDxcCursor*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetExtent"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetExtent(IDxcSourceRange** pRange)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcSourceRange**, int>)(lpVtbl[3]))((IDxcCursor*)Unsafe.AsPointer(ref this), pRange);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetLocation(IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetKind"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetKind(DxcCursorKind* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, DxcCursorKind*, int>)(lpVtbl[5]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetKindFlags"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetKindFlags(DxcCursorKindFlags* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, DxcCursorKindFlags*, int>)(lpVtbl[6]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetSemanticParent"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetSemanticParent(IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcCursor**, int>)(lpVtbl[7]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetLexicalParent"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetLexicalParent(IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcCursor**, int>)(lpVtbl[8]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetCursorType"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetCursorType(IDxcType** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcType**, int>)(lpVtbl[9]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetNumArguments"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumArguments(int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, int*, int>)(lpVtbl[10]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetArgumentAt"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetArgumentAt(int index, IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, int, IDxcCursor**, int>)(lpVtbl[11]))((IDxcCursor*)Unsafe.AsPointer(ref this), index, pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetReferencedCursor"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetReferencedCursor(IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcCursor**, int>)(lpVtbl[12]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetDefinitionCursor"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefinitionCursor(IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcCursor**, int>)(lpVtbl[13]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.FindReferencesInFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int FindReferencesInFile(IDxcFile* file, [NativeTypeName("unsigned int")] uint skip, [NativeTypeName("unsigned int")] uint top, [NativeTypeName("unsigned int *")] uint* pResultLength, IDxcCursor*** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcFile*, uint, uint, uint*, IDxcCursor***, int>)(lpVtbl[14]))((IDxcCursor*)Unsafe.AsPointer(ref this), file, skip, top, pResultLength, pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetSpelling"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, sbyte**, int>)(lpVtbl[15]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.IsEqualTo"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int IsEqualTo(IDxcCursor* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcCursor*, int*, int>)(lpVtbl[16]))((IDxcCursor*)Unsafe.AsPointer(ref this), other, pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.IsNull"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(17)]
    [return: NativeTypeName("HRESULT")]
    public int IsNull([NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, int*, int>)(lpVtbl[17]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.IsDefinition"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(18)]
    [return: NativeTypeName("HRESULT")]
    public int IsDefinition([NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, int*, int>)(lpVtbl[18]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetDisplayName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(19)]
    [return: NativeTypeName("HRESULT")]
    public int GetDisplayName([NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, char**, int>)(lpVtbl[19]))((IDxcCursor*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetQualifiedName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(20)]
    [return: NativeTypeName("HRESULT")]
    public int GetQualifiedName([NativeTypeName("BOOL")] int includeTemplateArgs, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, int, char**, int>)(lpVtbl[20]))((IDxcCursor*)Unsafe.AsPointer(ref this), includeTemplateArgs, pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetFormattedName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(21)]
    [return: NativeTypeName("HRESULT")]
    public int GetFormattedName(DxcCursorFormatting formatting, [NativeTypeName("BSTR *")] char** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, DxcCursorFormatting, char**, int>)(lpVtbl[21]))((IDxcCursor*)Unsafe.AsPointer(ref this), formatting, pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetChildren"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(22)]
    [return: NativeTypeName("HRESULT")]
    public int GetChildren([NativeTypeName("unsigned int")] uint skip, [NativeTypeName("unsigned int")] uint top, [NativeTypeName("unsigned int *")] uint* pResultLength, IDxcCursor*** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, uint, uint, uint*, IDxcCursor***, int>)(lpVtbl[22]))((IDxcCursor*)Unsafe.AsPointer(ref this), skip, top, pResultLength, pResult);
    }

    /// <include file='IDxcCursor.xml' path='doc/member[@name="IDxcCursor.GetSnappedChild"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(23)]
    [return: NativeTypeName("HRESULT")]
    public int GetSnappedChild(IDxcSourceLocation* location, IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcCursor*, IDxcSourceLocation*, IDxcCursor**, int>)(lpVtbl[23]))((IDxcCursor*)Unsafe.AsPointer(ref this), location, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetExtent(IDxcSourceRange** pRange);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetLocation(IDxcSourceLocation** pResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetKind(DxcCursorKind* pResult);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetKindFlags(DxcCursorKindFlags* pResult);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetSemanticParent(IDxcCursor** pResult);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetLexicalParent(IDxcCursor** pResult);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetCursorType(IDxcType** pResult);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int GetNumArguments(int* pResult);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetArgumentAt(int index, IDxcCursor** pResult);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetReferencedCursor(IDxcCursor** pResult);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetDefinitionCursor(IDxcCursor** pResult);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int FindReferencesInFile(IDxcFile* file, [NativeTypeName("unsigned int")] uint skip, [NativeTypeName("unsigned int")] uint top, [NativeTypeName("unsigned int *")] uint* pResultLength, IDxcCursor*** pResult);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetSpelling([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int IsEqualTo(IDxcCursor* other, [NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(17)]
        [return: NativeTypeName("HRESULT")]
        int IsNull([NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(18)]
        [return: NativeTypeName("HRESULT")]
        int IsDefinition([NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(19)]
        [return: NativeTypeName("HRESULT")]
        int GetDisplayName([NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(20)]
        [return: NativeTypeName("HRESULT")]
        int GetQualifiedName([NativeTypeName("BOOL")] int includeTemplateArgs, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(21)]
        [return: NativeTypeName("HRESULT")]
        int GetFormattedName(DxcCursorFormatting formatting, [NativeTypeName("BSTR *")] char** pResult);

        [VtblIndex(22)]
        [return: NativeTypeName("HRESULT")]
        int GetChildren([NativeTypeName("unsigned int")] uint skip, [NativeTypeName("unsigned int")] uint top, [NativeTypeName("unsigned int *")] uint* pResultLength, IDxcCursor*** pResult);

        [VtblIndex(23)]
        [return: NativeTypeName("HRESULT")]
        int GetSnappedChild(IDxcSourceLocation* location, IDxcCursor** pResult);
    }

    public partial struct Vtbl<TSelf>
        where TSelf : unmanaged, Interface
    {
        [NativeTypeName("HRESULT (const IID &, void **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, Guid*, void**, int> QueryInterface;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> AddRef;

        [NativeTypeName("ULONG () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> Release;

        [NativeTypeName("HRESULT (IDxcSourceRange **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceRange**, int> GetExtent;

        [NativeTypeName("HRESULT (IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation**, int> GetLocation;

        [NativeTypeName("HRESULT (DxcCursorKind *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcCursorKind*, int> GetKind;

        [NativeTypeName("HRESULT (DxcCursorKindFlags *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcCursorKindFlags*, int> GetKindFlags;

        [NativeTypeName("HRESULT (IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCursor**, int> GetSemanticParent;

        [NativeTypeName("HRESULT (IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCursor**, int> GetLexicalParent;

        [NativeTypeName("HRESULT (IDxcType **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcType**, int> GetCursorType;

        [NativeTypeName("HRESULT (int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, int> GetNumArguments;

        [NativeTypeName("HRESULT (int, IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int, IDxcCursor**, int> GetArgumentAt;

        [NativeTypeName("HRESULT (IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCursor**, int> GetReferencedCursor;

        [NativeTypeName("HRESULT (IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCursor**, int> GetDefinitionCursor;

        [NativeTypeName("HRESULT (IDxcFile *, unsigned int, unsigned int, unsigned int *, IDxcCursor ***) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile*, uint, uint, uint*, IDxcCursor***, int> FindReferencesInFile;

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetSpelling;

        [NativeTypeName("HRESULT (IDxcCursor *, BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCursor*, int*, int> IsEqualTo;

        [NativeTypeName("HRESULT (BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, int> IsNull;

        [NativeTypeName("HRESULT (BOOL *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int*, int> IsDefinition;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetDisplayName;

        [NativeTypeName("HRESULT (BOOL, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, int, char**, int> GetQualifiedName;

        [NativeTypeName("HRESULT (DxcCursorFormatting, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcCursorFormatting, char**, int> GetFormattedName;

        [NativeTypeName("HRESULT (unsigned int, unsigned int, unsigned int *, IDxcCursor ***) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint, uint*, IDxcCursor***, int> GetChildren;

        [NativeTypeName("HRESULT (IDxcSourceLocation *, IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation*, IDxcCursor**, int> GetSnappedChild;
    }
}
