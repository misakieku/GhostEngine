using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

[Guid("937824A0-7F5A-4815-9BA7-7FC0424F4173")]
[NativeTypeName("struct IDxcIndex : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcIndex : IDxcIndex.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcIndex);

    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    public HRESULT QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, Guid*, void**, int>)(lpVtbl[0]))((IDxcIndex*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, uint>)(lpVtbl[1]))((IDxcIndex*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, uint>)(lpVtbl[2]))((IDxcIndex*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    public HRESULT SetGlobalOptions(DxcGlobalOptions options)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, DxcGlobalOptions, int>)(lpVtbl[3]))((IDxcIndex*)Unsafe.AsPointer(ref this), options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    public HRESULT GetGlobalOptions(DxcGlobalOptions* options)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, DxcGlobalOptions*, int>)(lpVtbl[4]))((IDxcIndex*)Unsafe.AsPointer(ref this), options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    public HRESULT ParseTranslationUnit([NativeTypeName("const char *")] sbyte* source_filename, [NativeTypeName("const char *const *")] sbyte** command_line_args, int num_command_line_args, IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files, DxcTranslationUnitFlags options, IDxcTranslationUnit** pTranslationUnit)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcIndex*, sbyte*, sbyte**, int, IDxcUnsavedFile**, uint, DxcTranslationUnitFlags, IDxcTranslationUnit**, int>)(lpVtbl[5]))((IDxcIndex*)Unsafe.AsPointer(ref this), source_filename, command_line_args, num_command_line_args, unsaved_files, num_unsaved_files, options, pTranslationUnit);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        HRESULT SetGlobalOptions(DxcGlobalOptions options);

        [VtblIndex(4)]
        HRESULT GetGlobalOptions(DxcGlobalOptions* options);

        [VtblIndex(5)]
        HRESULT ParseTranslationUnit([NativeTypeName("const char *")] sbyte* source_filename, [NativeTypeName("const char *const *")] sbyte** command_line_args, int num_command_line_args, IDxcUnsavedFile** unsaved_files, [NativeTypeName("unsigned int")] uint num_unsaved_files, DxcTranslationUnitFlags options, IDxcTranslationUnit** pTranslationUnit);
    }
}
