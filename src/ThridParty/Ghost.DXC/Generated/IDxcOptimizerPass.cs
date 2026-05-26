using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcOptimizerPass.xml' path='doc/member[@name="IDxcOptimizerPass"]/*' />
[Guid("AE2CD79F-CC22-453F-9B6B-B124E7A5204C")]
[NativeTypeName("struct IDxcOptimizerPass : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcOptimizerPass : IDxcOptimizerPass.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcOptimizerPass);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, Guid*, void**, int>)(lpVtbl[0]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, uint>)(lpVtbl[1]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, uint>)(lpVtbl[2]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcOptimizerPass.xml' path='doc/member[@name="IDxcOptimizerPass.GetOptionName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetOptionName([NativeTypeName("LPWSTR *")] char** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, char**, int>)(lpVtbl[3]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcOptimizerPass.xml' path='doc/member[@name="IDxcOptimizerPass.GetDescription"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetDescription([NativeTypeName("LPWSTR *")] char** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, char**, int>)(lpVtbl[4]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this), ppResult);
    }

    /// <include file='IDxcOptimizerPass.xml' path='doc/member[@name="IDxcOptimizerPass.GetOptionArgCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetOptionArgCount([NativeTypeName("UINT32 *")] uint* pCount)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, uint*, int>)(lpVtbl[5]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this), pCount);
    }

    /// <include file='IDxcOptimizerPass.xml' path='doc/member[@name="IDxcOptimizerPass.GetOptionArgName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetOptionArgName([NativeTypeName("UINT32")] uint argIndex, [NativeTypeName("LPWSTR *")] char** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, uint, char**, int>)(lpVtbl[6]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this), argIndex, ppResult);
    }

    /// <include file='IDxcOptimizerPass.xml' path='doc/member[@name="IDxcOptimizerPass.GetOptionArgDescription"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetOptionArgDescription([NativeTypeName("UINT32")] uint argIndex, [NativeTypeName("LPWSTR *")] char** ppResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcOptimizerPass*, uint, char**, int>)(lpVtbl[7]))((IDxcOptimizerPass*)Unsafe.AsPointer(ref this), argIndex, ppResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetOptionName([NativeTypeName("LPWSTR *")] char** ppResult);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetDescription([NativeTypeName("LPWSTR *")] char** ppResult);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetOptionArgCount([NativeTypeName("UINT32 *")] uint* pCount);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetOptionArgName([NativeTypeName("UINT32")] uint argIndex, [NativeTypeName("LPWSTR *")] char** ppResult);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetOptionArgDescription([NativeTypeName("UINT32")] uint argIndex, [NativeTypeName("LPWSTR *")] char** ppResult);
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

        [NativeTypeName("HRESULT (LPWSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetOptionName;

        [NativeTypeName("HRESULT (LPWSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetDescription;

        [NativeTypeName("HRESULT (UINT32 *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint*, int> GetOptionArgCount;

        [NativeTypeName("HRESULT (UINT32, LPWSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetOptionArgName;

        [NativeTypeName("HRESULT (UINT32, LPWSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetOptionArgDescription;
    }
}
