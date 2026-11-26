using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("9677DEE0-C0E5-46A1-8B40-3DB3168BE63D")]
[NativeTypeName("struct IDxcTranslationUnit : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcTranslationUnit : IDxcTranslationUnit.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcTranslationUnit);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, Guid*, void**, int>)(lpVtbl[0]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint>)(lpVtbl[1]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint>)(lpVtbl[2]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetCursor(IDxcCursor** pCursor)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcCursor**, int>)(lpVtbl[3]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pCursor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT Tokenize(IDxcSourceRange* range, IDxcToken*** pTokens, [NativeTypeName("unsigned int *")] uint* pTokenCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcSourceRange*, IDxcToken***, uint*, int>)(lpVtbl[4]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), range, pTokens, pTokenCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT GetLocation(IDxcFile* file, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcFile*, uint, uint, IDxcSourceLocation**, int>)(lpVtbl[5]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), file, line, column, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetNumDiagnostics([NativeTypeName("unsigned int *")] uint* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint*, int>)(lpVtbl[6]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    public HRESULT GetDiagnostic([NativeTypeName("unsigned int")] uint index, IDxcDiagnostic** pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint, IDxcDiagnostic**, int>)(lpVtbl[7]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), index, pValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    public HRESULT GetFile([NativeTypeName("const char *")] sbyte* name, IDxcFile** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, sbyte*, IDxcFile**, int>)(lpVtbl[8]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), name, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    public HRESULT GetFileName([NativeTypeName("LPSTR *")] sbyte** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, sbyte**, int>)(lpVtbl[9]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(10)]
    public HRESULT Reparse(IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcUnsavedFile**, uint, int>)(lpVtbl[10]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), unsaved_files, num_unsaved_files);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(11)]
    public HRESULT GetCursorForLocation(IDxcSourceLocation* location, IDxcCursor** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcSourceLocation*, IDxcCursor**, int>)(lpVtbl[11]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), location, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(12)]
    public HRESULT GetLocationForOffset(IDxcFile* file, [NativeTypeName("unsigned int")] uint offset, IDxcSourceLocation** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcFile*, uint, IDxcSourceLocation**, int>)(lpVtbl[12]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), file, offset, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(13)]
    public HRESULT GetSkippedRanges(IDxcFile* file, [NativeTypeName("unsigned int *")] uint* pResultCount, IDxcSourceRange*** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, IDxcFile*, uint*, IDxcSourceRange***, int>)(lpVtbl[13]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), file, pResultCount, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(14)]
    public HRESULT GetDiagnosticDetails([NativeTypeName("unsigned int")] uint index, DxcDiagnosticDisplayOptions options, [NativeTypeName("unsigned int *")] uint* errorCode, [NativeTypeName("unsigned int *")] uint* errorLine, [NativeTypeName("unsigned int *")] uint* errorColumn, [NativeTypeName("BSTR *")] char** errorFile, [NativeTypeName("unsigned int *")] uint* errorOffset, [NativeTypeName("unsigned int *")] uint* errorLength, [NativeTypeName("BSTR *")] char** errorMessage)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint, DxcDiagnosticDisplayOptions, uint*, uint*, uint*, char**, uint*, uint*, char**, int>)(lpVtbl[14]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), index, options, errorCode, errorLine, errorColumn, errorFile, errorOffset, errorLength, errorMessage);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(15)]
    public HRESULT GetInclusionList([NativeTypeName("unsigned int *")] uint* pResultCount, IDxcInclusion*** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, uint*, IDxcInclusion***, int>)(lpVtbl[15]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), pResultCount, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(16)]
    public HRESULT CodeCompleteAt([NativeTypeName("const char *")] sbyte* fileName, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcUnsavedFile** pUnsavedFiles, [NativeTypeName("unsigned int")] uint numUnsavedFiles, DxcCodeCompleteFlags options, IDxcCodeCompleteResults** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcTranslationUnit*, sbyte*, uint, uint, IDxcUnsavedFile**, uint, DxcCodeCompleteFlags, IDxcCodeCompleteResults**, int>)(lpVtbl[16]))((IDxcTranslationUnit*)Unsafe.AsPointer(ref this), fileName, line, column, pUnsavedFiles, numUnsavedFiles, options, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetCursor(IDxcCursor** pCursor);

        [VtblIndex(4)]
        HRESULT Tokenize(IDxcSourceRange* range, IDxcToken*** pTokens, [NativeTypeName("unsigned int *")] uint* pTokenCount);

        [VtblIndex(5)]
        HRESULT GetLocation(IDxcFile* file, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcSourceLocation** pResult);

        [VtblIndex(6)]
        HRESULT GetNumDiagnostics([NativeTypeName("unsigned int *")] uint* pValue);

        [VtblIndex(7)]
        HRESULT GetDiagnostic([NativeTypeName("unsigned int")] uint index, IDxcDiagnostic** pValue);

        [VtblIndex(8)]
        HRESULT GetFile([NativeTypeName("const char *")] sbyte* name, IDxcFile** pResult);

        [VtblIndex(9)]
        HRESULT GetFileName([NativeTypeName("LPSTR *")] sbyte** pResult);

        [VtblIndex(10)]
        HRESULT Reparse(IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files);

        [VtblIndex(11)]
        HRESULT GetCursorForLocation(IDxcSourceLocation* location, IDxcCursor** pResult);

        [VtblIndex(12)]
        HRESULT GetLocationForOffset(IDxcFile* file, [NativeTypeName("unsigned int")] uint offset, IDxcSourceLocation** pResult);

        [VtblIndex(13)]
        HRESULT GetSkippedRanges(IDxcFile* file, [NativeTypeName("unsigned int *")] uint* pResultCount, IDxcSourceRange*** pResult);

        [VtblIndex(14)]
        HRESULT GetDiagnosticDetails([NativeTypeName("unsigned int")] uint index, DxcDiagnosticDisplayOptions options, [NativeTypeName("unsigned int *")] uint* errorCode, [NativeTypeName("unsigned int *")] uint* errorLine, [NativeTypeName("unsigned int *")] uint* errorColumn, [NativeTypeName("BSTR *")] char** errorFile, [NativeTypeName("unsigned int *")] uint* errorOffset, [NativeTypeName("unsigned int *")] uint* errorLength, [NativeTypeName("BSTR *")] char** errorMessage);

        [VtblIndex(15)]
        HRESULT GetInclusionList([NativeTypeName("unsigned int *")] uint* pResultCount, IDxcInclusion*** pResult);

        [VtblIndex(16)]
        HRESULT CodeCompleteAt([NativeTypeName("const char *")] sbyte* fileName, [NativeTypeName("unsigned int")] uint line, [NativeTypeName("unsigned int")] uint column, IDxcUnsavedFile** pUnsavedFiles, [NativeTypeName("unsigned int")] uint numUnsavedFiles, DxcCodeCompleteFlags options, IDxcCodeCompleteResults** pResult);
    }
}
