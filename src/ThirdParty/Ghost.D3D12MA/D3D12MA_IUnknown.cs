using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12MA;

internal static unsafe class D3D12MANativeGuids
{
    internal static Guid* NullGuid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data =
            [
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            ];

            return (Guid*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        }
    }
}

public unsafe partial struct D3D12MA_Allocator : IUnknown.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => D3D12MANativeGuids.NullGuid;
}

public unsafe partial struct D3D12MA_Allocation : IUnknown.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => D3D12MANativeGuids.NullGuid;
}

public unsafe partial struct D3D12MA_Pool : IUnknown.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => D3D12MANativeGuids.NullGuid;
}

public unsafe partial struct D3D12MA_DefragmentationContext : IUnknown.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => D3D12MANativeGuids.NullGuid;
}

public unsafe partial struct D3D12MA_VirtualBlock : IUnknown.Interface, INativeGuid
{
    static Guid* INativeGuid.NativeGuid => D3D12MANativeGuids.NullGuid;
}
