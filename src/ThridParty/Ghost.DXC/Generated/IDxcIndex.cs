using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcIndex.xml' path='doc/member[@name="IDxcIndex"]/*' />
[Guid("937824A0-7F5A-4815-9BA7-7FC0424F4173")]
[NativeTypeName("struct IDxcIndex : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcIndex : IDxcIndex.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcIndex);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, Guid*, void**, int>)(lpVtbl[0]))((IDxcIndex*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, uint>)(lpVtbl[1]))((IDxcIndex*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, uint>)(lpVtbl[2]))((IDxcIndex*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcIndex.xml' path='doc/member[@name="IDxcIndex.SetGlobalOptions"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int SetGlobalOptions(DxcGlobalOptions options)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, DxcGlobalOptions, int>)(lpVtbl[3]))((IDxcIndex*)Unsafe.AsPointer(ref this), options);
    }

    /// <include file='IDxcIndex.xml' path='doc/member[@name="IDxcIndex.GetGlobalOptions"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int GetGlobalOptions(DxcGlobalOptions* options)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, DxcGlobalOptions*, int>)(lpVtbl[4]))((IDxcIndex*)Unsafe.AsPointer(ref this), options);
    }

    /// <include file='IDxcIndex.xml' path='doc/member[@name="IDxcIndex.ParseTranslationUnit"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int ParseTranslationUnit([NativeTypeName("const char *")] sbyte* source_filename, [NativeTypeName("const char *const *")] sbyte** command_line_args, int num_command_line_args, IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files, DxcTranslationUnitFlags options, IDxcTranslationUnit** pTranslationUnit)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, sbyte*, sbyte**, int, IDxcUnsavedFile**, uint, DxcTranslationUnitFlags, IDxcTranslationUnit**, int>)(lpVtbl[5]))((IDxcIndex*)Unsafe.AsPointer(ref this), source_filename, command_line_args, num_command_line_args, unsaved_files, num_unsaved_files, options, pTranslationUnit);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int SetGlobalOptions(DxcGlobalOptions options);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int GetGlobalOptions(DxcGlobalOptions* options);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int ParseTranslationUnit([NativeTypeName("const char *")] sbyte* source_filename, [NativeTypeName("const char *const *")] sbyte** command_line_args, int num_command_line_args, IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files, DxcTranslationUnitFlags options, IDxcTranslationUnit** pTranslationUnit);
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

        [NativeTypeName("HRESULT (DxcGlobalOptions) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcGlobalOptions, int> SetGlobalOptions;

        [NativeTypeName("HRESULT (DxcGlobalOptions *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, DxcGlobalOptions*, int> GetGlobalOptions;

        [NativeTypeName("HRESULT (const char *, const char *const *, int, IDxcUnsavedFile **, unsigned int, DxcTranslationUnitFlags, IDxcTranslationUnit **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, sbyte*, sbyte**, int, IDxcUnsavedFile**, uint, DxcTranslationUnitFlags, IDxcTranslationUnit**, int> ParseTranslationUnit;
    }
}
