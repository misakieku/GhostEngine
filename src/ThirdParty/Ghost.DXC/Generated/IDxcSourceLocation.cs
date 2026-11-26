using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("8E7DDF1C-D7D3-4D69-B286-85FCCBA1E0CF")]
[NativeTypeName("struct IDxcSourceLocation : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcSourceLocation : IDxcSourceLocation.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcSourceLocation);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, Guid*, void**, int>)(lpVtbl[0]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, uint>)(lpVtbl[1]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, uint>)(lpVtbl[2]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT IsEqualTo(IDxcSourceLocation* other, [NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, IDxcSourceLocation*, int*, int>)(lpVtbl[3]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), other, pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetSpellingLocation(IDxcFile** pFile, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol, [NativeTypeName("unsigned int *")] uint* pOffset)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, IDxcFile**, uint*, uint*, uint*, int>)(lpVtbl[4]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), pFile, pLine, pCol, pOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT IsNull([NativeTypeName("BOOL *")] int* pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, int*, int>)(lpVtbl[5]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), pResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    public HRESULT GetPresumedLocation([NativeTypeName("LPSTR *")] sbyte** pFilename, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcSourceLocation*, sbyte**, uint*, uint*, int>)(lpVtbl[6]))((IDxcSourceLocation*)Unsafe.AsPointer(ref this), pFilename, pLine, pCol);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT IsEqualTo(IDxcSourceLocation* other, [NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(4)]
        HRESULT GetSpellingLocation(IDxcFile** pFile, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol, [NativeTypeName("unsigned int *")] uint* pOffset);

        [VtblIndex(5)]
        HRESULT IsNull([NativeTypeName("BOOL *")] int* pResult);

        [VtblIndex(6)]
        HRESULT GetPresumedLocation([NativeTypeName("LPSTR *")] sbyte** pFilename, [NativeTypeName("unsigned int *")] uint* pLine, [NativeTypeName("unsigned int *")] uint* pCol);
    }
}
