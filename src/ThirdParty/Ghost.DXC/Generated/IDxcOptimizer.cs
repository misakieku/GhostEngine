using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("25740E2E-9CBA-401B-9119-4FB42F39F270")]
[NativeTypeName("struct IDxcOptimizer : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcOptimizer : IDxcOptimizer.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcOptimizer);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizer*, Guid*, void**, int>)(lpVtbl[0]))((IDxcOptimizer*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizer*, uint>)(lpVtbl[1]))((IDxcOptimizer*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizer*, uint>)(lpVtbl[2]))((IDxcOptimizer*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT GetAvailablePassCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizer*, uint*, int>)(lpVtbl[3]))((IDxcOptimizer*)Unsafe.AsPointer(ref this), pCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetAvailablePass([NativeTypeName("UINT32")] uint index, IDxcOptimizerPass** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizer*, uint, IDxcOptimizerPass**, int>)(lpVtbl[4]))((IDxcOptimizer*)Unsafe.AsPointer(ref this), index, ppResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT RunOptimizer(IDxcBlob* pBlob, [NativeTypeName("LPCWSTR *")] char** ppOptions, [NativeTypeName("UINT32")] uint optionCount, IDxcBlob** pOutputModule, IDxcBlobEncoding** ppOutputText)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizer*, IDxcBlob*, char**, uint, IDxcBlob**, IDxcBlobEncoding**, int>)(lpVtbl[5]))((IDxcOptimizer*)Unsafe.AsPointer(ref this), pBlob, ppOptions, optionCount, pOutputModule, ppOutputText);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT GetAvailablePassCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(4)]
        HRESULT GetAvailablePass([NativeTypeName("UINT32")] uint index, IDxcOptimizerPass** ppResult);

        [VtblIndex(5)]
        HRESULT RunOptimizer(IDxcBlob* pBlob, [NativeTypeName("LPCWSTR *")] char** ppOptions, [NativeTypeName("UINT32")] uint optionCount, IDxcBlob** pOutputModule, IDxcBlobEncoding** ppOutputText);
    }
}
