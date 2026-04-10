using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit"]/*' />
[Guid("9677DEE0-C0E5-46A1-8B40-3DB3168BE63D")]
[NativeTypeName("struct IDxcTranslationUnit : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcTranslationUnit : IDxcTranslationUnit.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcTranslationUnit);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, Guid*, void**, int>)(lpVtbl[0]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint>)(lpVtbl[1]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint>)(lpVtbl[2]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetCursor"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetCursor(IDxcCursor** pCursor)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcCursor**, int>)(lpVtbl[3]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pCursor);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.Tokenize"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int Tokenize(IDxcSourceRange* range, IDxcToken*** pTokens, [NativeTypeName("unsigned int *")] uint* pTokenCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcSourceRange*, IDxcToken***, uint*, int>)(lpVtbl[4]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), range, pTokens, pTokenCount);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetLocation(IDxcFile* file, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcFile*, uint, uint, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), file, line, column, pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetNumDiagnostics"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetNumDiagnostics([NativeTypeName("unsigned int *")] uint* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint*, int>)(lpVtbl[6]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetDiagnostic"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDiagnostic([NativeTypeName("unsigned int")] uint index, IDxcDiagnostic** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint, IDxcDiagnostic**, int>)(lpVtbl[7]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), index, pValue);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetFile([NativeTypeName("const char *")] sbyte* name, IDxcFile** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, sbyte*, IDxcFile**, int>)(lpVtbl[8]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), name, pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetFileName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int GetFileName([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, sbyte**, int>)(lpVtbl[9]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.Reparse"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    [return: NativeTypeName("HRESULT")]
    public int Reparse(IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcUnsavedFile**, uint, int>)(lpVtbl[10]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), unsaved_files, num_unsaved_files);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetCursorForLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    [return: NativeTypeName("HRESULT")]
    public int GetCursorForLocation(IDxcSourceLocation* location, IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcSourceLocation*, IDxcCursor**, int>)(lpVtbl[11]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), location, pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetLocationForOffset"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    [return: NativeTypeName("HRESULT")]
    public int GetLocationForOffset(IDxcFile* file, [NativeTypeName("unsigned int")] uint offset, IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcFile*, uint, IDxcSourceLocation**, int>)(lpVtbl[12]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), file, offset, pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetSkippedRanges"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    [return: NativeTypeName("HRESULT")]
    public int GetSkippedRanges(IDxcFile* file, [NativeTypeName("unsigned int *")] uint* pResultCount, IDxcSourceRange*** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcFile*, uint*, IDxcSourceRange***, int>)(lpVtbl[13]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), file, pResultCount, pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetDiagnosticDetails"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    [return: NativeTypeName("HRESULT")]
    public int GetDiagnosticDetails([NativeTypeName("unsigned int")] uint index, DxcDiagnosticDisplayOptions options, [NativeTypeName("unsigned int *")] uint* errorCode, [NativeTypeName("unsigned int *")] uint* errorLine, [NativeTypeName("unsigned int *")] uint* errorColumn, [NativeTypeName("BSTR *")] char** errorFile, [NativeTypeName("unsigned int *")] uint* errorOffset, [NativeTypeName("unsigned int *")] uint* errorLength, [NativeTypeName("BSTR *")] char** errorMessage)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint, DxcDiagnosticDisplayOptions, uint*, uint*, uint*, char**, uint*, uint*, char**, int>)(lpVtbl[14]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), index, options, errorCode, errorLine, errorColumn, errorFile, errorOffset, errorLength, errorMessage);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.GetInclusionList"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    [return: NativeTypeName("HRESULT")]
    public int GetInclusionList([NativeTypeName("unsigned int *")] uint* pResultCount, IDxcInclusion*** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint*, IDxcInclusion***, int>)(lpVtbl[15]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pResultCount, pResult);
    }

    /// <include file='IDxcTranslationUnit.xml' path='doc/member[@name="IDxcTranslationUnit.CodeCompleteAt"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    [return: NativeTypeName("HRESULT")]
    public int CodeCompleteAt([NativeTypeName("const char *")] sbyte* fileName, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcUnsavedFile** pUnsavedFiles, [NativeTypeName("unsigned int")] uint numUnsavedFiles, DxcCodeCompleteFlags options, IDxcCodeCompleteResults** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, sbyte*, uint, uint, IDxcUnsavedFile**, uint, DxcCodeCompleteFlags, IDxcCodeCompleteResults**, int>)(lpVtbl[16]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), fileName, line, column, pUnsavedFiles, numUnsavedFiles, options, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetCursor(IDxcCursor** pCursor);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int Tokenize(IDxcSourceRange* range, IDxcToken*** pTokens, [NativeTypeName("unsigned int *")] uint* pTokenCount);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetLocation(IDxcFile* file, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcSourceLocation** pResult);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetNumDiagnostics([NativeTypeName("unsigned int *")] uint* pValue);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetDiagnostic([NativeTypeName("unsigned int")] uint index, IDxcDiagnostic** pValue);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetFile([NativeTypeName("const char *")] sbyte* name, IDxcFile** pResult);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int GetFileName([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(10)]
        [return: NativeTypeName("HRESULT")]
        int Reparse(IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files);

        [VtblIndex(11)]
        [return: NativeTypeName("HRESULT")]
        int GetCursorForLocation(IDxcSourceLocation* location, IDxcCursor** pResult);

        [VtblIndex(12)]
        [return: NativeTypeName("HRESULT")]
        int GetLocationForOffset(IDxcFile* file, [NativeTypeName("unsigned int")] uint offset, IDxcSourceLocation** pResult);

        [VtblIndex(13)]
        [return: NativeTypeName("HRESULT")]
        int GetSkippedRanges(IDxcFile* file, [NativeTypeName("unsigned int *")] uint* pResultCount, IDxcSourceRange*** pResult);

        [VtblIndex(14)]
        [return: NativeTypeName("HRESULT")]
        int GetDiagnosticDetails([NativeTypeName("unsigned int")] uint index, DxcDiagnosticDisplayOptions options, [NativeTypeName("unsigned int *")] uint* errorCode, [NativeTypeName("unsigned int *")] uint* errorLine, [NativeTypeName("unsigned int *")] uint* errorColumn, [NativeTypeName("BSTR *")] char** errorFile, [NativeTypeName("unsigned int *")] uint* errorOffset, [NativeTypeName("unsigned int *")] uint* errorLength, [NativeTypeName("BSTR *")] char** errorMessage);

        [VtblIndex(15)]
        [return: NativeTypeName("HRESULT")]
        int GetInclusionList([NativeTypeName("unsigned int *")] uint* pResultCount, IDxcInclusion*** pResult);

        [VtblIndex(16)]
        [return: NativeTypeName("HRESULT")]
        int CodeCompleteAt([NativeTypeName("const char *")] sbyte* fileName, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcUnsavedFile** pUnsavedFiles, [NativeTypeName("unsigned int")] uint numUnsavedFiles, DxcCodeCompleteFlags options, IDxcCodeCompleteResults** pResult);
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

        [NativeTypeName("HRESULT (IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcCursor**, int> GetCursor;

        [NativeTypeName("HRESULT (IDxcSourceRange *, IDxcToken ***, unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceRange*, IDxcToken***, uint*, int> Tokenize;

        [NativeTypeName("HRESULT (IDxcFile *, unsigned int, unsigned int, IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile*, uint, uint, IDxcSourceLocation**, int> GetLocation;

        [NativeTypeName("HRESULT (unsigned int *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetNumDiagnostics;

        [NativeTypeName("HRESULT (unsigned int, IDxcDiagnostic **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcDiagnostic**, int> GetDiagnostic;

        [NativeTypeName("HRESULT (const char *, IDxcFile **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte*, IDxcFile**, int> GetFile;

        [NativeTypeName("HRESULT (LPSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte**, int> GetFileName;

        [NativeTypeName("HRESULT (IDxcUnsavedFile **, unsigned int) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcUnsavedFile**, uint, int> Reparse;

        [NativeTypeName("HRESULT (IDxcSourceLocation *, IDxcCursor **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation*, IDxcCursor**, int> GetCursorForLocation;

        [NativeTypeName("HRESULT (IDxcFile *, unsigned int, IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile*, uint, IDxcSourceLocation**, int> GetLocationForOffset;

        [NativeTypeName("HRESULT (IDxcFile *, unsigned int *, IDxcSourceRange ***) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcFile*, uint*, IDxcSourceRange***, int> GetSkippedRanges;

        [NativeTypeName("HRESULT (unsigned int, DxcDiagnosticDisplayOptions, unsigned int *, unsigned int *, unsigned int *, BSTR *, unsigned int *, unsigned int *, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, DxcDiagnosticDisplayOptions, uint*, uint*, uint*, char**, uint*, uint*, char**, int> GetDiagnosticDetails;

        [NativeTypeName("HRESULT (unsigned int *, IDxcInclusion ***) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, IDxcInclusion***, int> GetInclusionList;

        [NativeTypeName("HRESULT (const char *, unsigned int, unsigned int, IDxcUnsavedFile **, unsigned int, DxcCodeCompleteFlags, IDxcCodeCompleteResults **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte*, uint, uint, IDxcUnsavedFile**, uint, DxcCodeCompleteFlags, IDxcCodeCompleteResults**, int> CodeCompleteAt;
    }
}
