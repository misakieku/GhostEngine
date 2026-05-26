using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixDxilInstructionOffsets.xml' path='doc/member[@name="IDxcPixDxilInstructionOffsets"]/*' />
[Guid("EB71F85E-8542-44B5-87DA-9D76045A1910")]
[NativeTypeName("struct IDxcPixDxilInstructionOffsets : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixDxilInstructionOffsets : IDxcPixDxilInstructionOffsets.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixDxilInstructionOffsets);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilInstructionOffsets*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixDxilInstructionOffsets*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilInstructionOffsets*, uint>)(lpVtbl[1]))((IDxcPixDxilInstructionOffsets*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilInstructionOffsets*, uint>)(lpVtbl[2]))((IDxcPixDxilInstructionOffsets*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilInstructionOffsets.xml' path='doc/member[@name="IDxcPixDxilInstructionOffsets.GetCount"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("DWORD")]
    public uint GetCount()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilInstructionOffsets*, uint>)(lpVtbl[3]))((IDxcPixDxilInstructionOffsets*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilInstructionOffsets.xml' path='doc/member[@name="IDxcPixDxilInstructionOffsets.GetOffsetByIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("DWORD")]
    public uint GetOffsetByIndex([NativeTypeName("DWORD")] uint Index)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilInstructionOffsets*, uint, uint>)(lpVtbl[4]))((IDxcPixDxilInstructionOffsets*)Unsafe.AsPointer(ref this), Index);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("DWORD")]
        uint GetCount();

        [VtblIndex(4)]
        [return: NativeTypeName("DWORD")]
        uint GetOffsetByIndex([NativeTypeName("DWORD")] uint Index);
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

        [NativeTypeName("DWORD () __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint> GetCount;

        [NativeTypeName("DWORD (DWORD) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint> GetOffsetByIndex;
    }
}
