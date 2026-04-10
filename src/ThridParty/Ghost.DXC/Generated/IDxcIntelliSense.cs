using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense"]/*' />
[Guid("B1F99513-46D6-4112-8169-DD0D6053F17D")]
[NativeTypeName("struct IDxcIntelliSense : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcIntelliSense : IDxcIntelliSense.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcIntelliSense);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, Guid*, void**, int>)(lpVtbl[0]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, uint>)(lpVtbl[1]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, uint>)(lpVtbl[2]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.CreateIndex"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int CreateIndex(IDxcIndex** index)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcIndex**, int>)(lpVtbl[3]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), index);
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.GetNullLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetNullLocation(IDxcSourceLocation** location)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcSourceLocation**, int>)(lpVtbl[4]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), location);
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.GetNullRange"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetNullRange(IDxcSourceRange** location)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcSourceRange**, int>)(lpVtbl[5]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), location);
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.GetRange"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetRange(IDxcSourceLocation* start, IDxcSourceLocation* end, IDxcSourceRange** location)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, IDxcSourceLocation*, IDxcSourceLocation*, IDxcSourceRange**, int>)(lpVtbl[6]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), start, end, location);
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.GetDefaultDiagnosticDisplayOptions"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefaultDiagnosticDisplayOptions(DxcDiagnosticDisplayOptions* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, DxcDiagnosticDisplayOptions*, int>)(lpVtbl[7]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.GetDefaultEditingTUOptions"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetDefaultEditingTUOptions(DxcTranslationUnitFlags* pValue)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, DxcTranslationUnitFlags*, int>)(lpVtbl[8]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), pValue);
    }

    /// <include file='IDxcIntelliSense.xml' path='doc/member[@name="IDxcIntelliSense.CreateUnsavedFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(9)]
    [return: NativeTypeName("HRESULT")]
    public int CreateUnsavedFile([NativeTypeName("LPCSTR")] sbyte* fileName, [NativeTypeName("LPCSTR")] sbyte* contents, [NativeTypeName("unsigned int")] uint contentLength, IDxcUnsavedFile** pResult)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIntelliSense*, sbyte*, sbyte*, uint, IDxcUnsavedFile**, int>)(lpVtbl[9]))((IDxcIntelliSense*)Unsafe.AsPointer(ref this), fileName, contents, contentLength, pResult);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int CreateIndex(IDxcIndex** index);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetNullLocation(IDxcSourceLocation** location);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetNullRange(IDxcSourceRange** location);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetRange(IDxcSourceLocation* start, IDxcSourceLocation* end, IDxcSourceRange** location);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetDefaultDiagnosticDisplayOptions(DxcDiagnosticDisplayOptions* pValue);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetDefaultEditingTUOptions(DxcTranslationUnitFlags* pValue);

        [VtblIndex(9)]
        [return: NativeTypeName("HRESULT")]
        int CreateUnsavedFile([NativeTypeName("LPCSTR")] sbyte* fileName, [NativeTypeName("LPCSTR")] sbyte* contents, [NativeTypeName("unsigned int")] uint contentLength, IDxcUnsavedFile** pResult);
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

        [NativeTypeName("HRESULT (IDxcIndex **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcIndex**, int> CreateIndex;

        [NativeTypeName("HRESULT (IDxcSourceLocation **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation**, int> GetNullLocation;

        [NativeTypeName("HRESULT (IDxcSourceRange **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceRange**, int> GetNullRange;

        [NativeTypeName("HRESULT (IDxcSourceLocation *, IDxcSourceLocation *, IDxcSourceRange **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, IDxcSourceLocation*, IDxcSourceLocation*, IDxcSourceRange**, int> GetRange;

        [NativeTypeName("HRESULT (DxcDiagnosticDisplayOptions *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcDiagnosticDisplayOptions*, int> GetDefaultDiagnosticDisplayOptions;

        [NativeTypeName("HRESULT (DxcTranslationUnitFlags *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcTranslationUnitFlags*, int> GetDefaultEditingTUOptions;

        [NativeTypeName("HRESULT (LPCSTR, LPCSTR, unsigned int, IDxcUnsavedFile **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte*, sbyte*, uint, IDxcUnsavedFile**, int> CreateUnsavedFile;
    }
}
