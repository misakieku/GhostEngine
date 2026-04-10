using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo"]/*' />
[Guid("61B16C95-8799-4ED8-BDB0-3B6C08A141B4")]
[NativeTypeName("struct IDxcPixCompilationInfo : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixCompilationInfo : IDxcPixCompilationInfo.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixCompilationInfo);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, uint>)(lpVtbl[1]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, uint>)(lpVtbl[2]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo.GetSourceFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetSourceFile([NativeTypeName("DWORD")] uint SourceFileOrdinal, [NativeTypeName("BSTR *")] char** pSourceName, [NativeTypeName("BSTR *")] char** pSourceContents)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, uint, char**, char**, int>)(lpVtbl[3]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), SourceFileOrdinal, pSourceName, pSourceContents);
    }

    /// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo.GetArguments"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetArguments([NativeTypeName("BSTR *")] char** pArguments)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, char**, int>)(lpVtbl[4]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), pArguments);
    }

    /// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo.GetMacroDefinitions"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetMacroDefinitions([NativeTypeName("BSTR *")] char** pMacroDefinitions)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, char**, int>)(lpVtbl[5]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), pMacroDefinitions);
    }

    /// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo.GetEntryPointFile"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetEntryPointFile([NativeTypeName("BSTR *")] char** pEntryPointFile)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, char**, int>)(lpVtbl[6]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), pEntryPointFile);
    }

    /// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo.GetHlslTarget"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int GetHlslTarget([NativeTypeName("BSTR *")] char** pHlslTarget)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, char**, int>)(lpVtbl[7]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), pHlslTarget);
    }

    /// <include file='IDxcPixCompilationInfo.xml' path='doc/member[@name="IDxcPixCompilationInfo.GetEntryPoint"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int GetEntryPoint([NativeTypeName("BSTR *")] char** pEntryPoint)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixCompilationInfo*, char**, int>)(lpVtbl[8]))((IDxcPixCompilationInfo*)Unsafe.AsPointer(ref this), pEntryPoint);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetSourceFile([NativeTypeName("DWORD")] uint SourceFileOrdinal, [NativeTypeName("BSTR *")] char** pSourceName, [NativeTypeName("BSTR *")] char** pSourceContents);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetArguments([NativeTypeName("BSTR *")] char** pArguments);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetMacroDefinitions([NativeTypeName("BSTR *")] char** pMacroDefinitions);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetEntryPointFile([NativeTypeName("BSTR *")] char** pEntryPointFile);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int GetHlslTarget([NativeTypeName("BSTR *")] char** pHlslTarget);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int GetEntryPoint([NativeTypeName("BSTR *")] char** pEntryPoint);
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

        [NativeTypeName("HRESULT (DWORD, BSTR *, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, char**, int> GetSourceFile;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetArguments;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetMacroDefinitions;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetEntryPointFile;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetHlslTarget;

        [NativeTypeName("HRESULT (BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char**, int> GetEntryPoint;
    }
}
