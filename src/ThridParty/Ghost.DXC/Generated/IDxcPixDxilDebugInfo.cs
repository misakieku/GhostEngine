using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.DXC.Api;

namespace Ghost.DXC;

/// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo"]/*' />
[Guid("B875638E-108A-4D90-A53A-68D63773CB38")]
[NativeTypeName("struct IDxcPixDxilDebugInfo : IUnknown")]
[NativeInheritance("IUnknown")]
public unsafe partial struct IDxcPixDxilDebugInfo : IDxcPixDxilDebugInfo.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => (Guid*)Unsafe.AsPointer(in IID_IDxcPixDxilDebugInfo);

    public void** lpVtbl;

    /// <inheritdoc cref="IUnknown.QueryInterface" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(0)]
    [return: NativeTypeName("HRESULT")]
    public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, Guid*, void**, int>)(lpVtbl[0]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    /// <inheritdoc cref="IUnknown.AddRef" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(1)]
    [return: NativeTypeName("ULONG")]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint>)(lpVtbl[1]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this));
    }

    /// <inheritdoc cref="IUnknown.Release" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(2)]
    [return: NativeTypeName("ULONG")]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint>)(lpVtbl[2]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this));
    }

    /// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo.GetLiveVariablesAt"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(3)]
    [return: NativeTypeName("HRESULT")]
    public int GetLiveVariablesAt([NativeTypeName("DWORD")] uint InstructionOffset, IDxcPixDxilLiveVariables** ppLiveVariables)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint, IDxcPixDxilLiveVariables**, int>)(lpVtbl[3]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), InstructionOffset, ppLiveVariables);
    }

    /// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo.IsVariableInRegister"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(4)]
    [return: NativeTypeName("HRESULT")]
    public int IsVariableInRegister([NativeTypeName("DWORD")] uint InstructionOffset, [NativeTypeName("const wchar_t *")] char* VariableName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint, char*, int>)(lpVtbl[4]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), InstructionOffset, VariableName);
    }

    /// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo.GetFunctionName"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(5)]
    [return: NativeTypeName("HRESULT")]
    public int GetFunctionName([NativeTypeName("DWORD")] uint InstructionOffset, [NativeTypeName("BSTR *")] char** ppFunctionName)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint, char**, int>)(lpVtbl[5]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), InstructionOffset, ppFunctionName);
    }

    /// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo.GetStackDepth"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(6)]
    [return: NativeTypeName("HRESULT")]
    public int GetStackDepth([NativeTypeName("DWORD")] uint InstructionOffset, [NativeTypeName("DWORD *")] uint* StackDepth)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint, uint*, int>)(lpVtbl[6]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), InstructionOffset, StackDepth);
    }

    /// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo.InstructionOffsetsFromSourceLocation"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(7)]
    [return: NativeTypeName("HRESULT")]
    public int InstructionOffsetsFromSourceLocation([NativeTypeName("const wchar_t *")] char* FileName, [NativeTypeName("DWORD")] uint SourceLine, [NativeTypeName("DWORD")] uint SourceColumn, IDxcPixDxilInstructionOffsets** ppOffsets)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, char*, uint, uint, IDxcPixDxilInstructionOffsets**, int>)(lpVtbl[7]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), FileName, SourceLine, SourceColumn, ppOffsets);
    }

    /// <include file='IDxcPixDxilDebugInfo.xml' path='doc/member[@name="IDxcPixDxilDebugInfo.SourceLocationsFromInstructionOffset"]/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [VtblIndex(8)]
    [return: NativeTypeName("HRESULT")]
    public int SourceLocationsFromInstructionOffset([NativeTypeName("DWORD")] uint InstructionOffset, IDxcPixDxilSourceLocations** ppSourceLocations)
    {
        return ((delegate* unmanaged[MemberFunction]<IDxcPixDxilDebugInfo*, uint, IDxcPixDxilSourceLocations**, int>)(lpVtbl[8]))((IDxcPixDxilDebugInfo*)Unsafe.AsPointer(ref this), InstructionOffset, ppSourceLocations);
    }

    public interface Interface : IUnknown.Interface
    {
        [VtblIndex(3)]
        [return: NativeTypeName("HRESULT")]
        int GetLiveVariablesAt([NativeTypeName("DWORD")] uint InstructionOffset, IDxcPixDxilLiveVariables** ppLiveVariables);

        [VtblIndex(4)]
        [return: NativeTypeName("HRESULT")]
        int IsVariableInRegister([NativeTypeName("DWORD")] uint InstructionOffset, [NativeTypeName("const wchar_t *")] char* VariableName);

        [VtblIndex(5)]
        [return: NativeTypeName("HRESULT")]
        int GetFunctionName([NativeTypeName("DWORD")] uint InstructionOffset, [NativeTypeName("BSTR *")] char** ppFunctionName);

        [VtblIndex(6)]
        [return: NativeTypeName("HRESULT")]
        int GetStackDepth([NativeTypeName("DWORD")] uint InstructionOffset, [NativeTypeName("DWORD *")] uint* StackDepth);

        [VtblIndex(7)]
        [return: NativeTypeName("HRESULT")]
        int InstructionOffsetsFromSourceLocation([NativeTypeName("const wchar_t *")] char* FileName, [NativeTypeName("DWORD")] uint SourceLine, [NativeTypeName("DWORD")] uint SourceColumn, IDxcPixDxilInstructionOffsets** ppOffsets);

        [VtblIndex(8)]
        [return: NativeTypeName("HRESULT")]
        int SourceLocationsFromInstructionOffset([NativeTypeName("DWORD")] uint InstructionOffset, IDxcPixDxilSourceLocations** ppSourceLocations);
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

        [NativeTypeName("HRESULT (DWORD, IDxcPixDxilLiveVariables **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcPixDxilLiveVariables**, int> GetLiveVariablesAt;

        [NativeTypeName("HRESULT (DWORD, const wchar_t *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char*, int> IsVariableInRegister;

        [NativeTypeName("HRESULT (DWORD, BSTR *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, char**, int> GetFunctionName;

        [NativeTypeName("HRESULT (DWORD, DWORD *) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, uint*, int> GetStackDepth;

        [NativeTypeName("HRESULT (const wchar_t *, DWORD, DWORD, IDxcPixDxilInstructionOffsets **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, char*, uint, uint, IDxcPixDxilInstructionOffsets**, int> InstructionOffsetsFromSourceLocation;

        [NativeTypeName("HRESULT (DWORD, IDxcPixDxilSourceLocations **) __attribute__((stdcall))")]
        public delegate* unmanaged[MemberFunction]<TSelf*, uint, IDxcPixDxilSourceLocations**, int> SourceLocationsFromInstructionOffset;
    }
}
