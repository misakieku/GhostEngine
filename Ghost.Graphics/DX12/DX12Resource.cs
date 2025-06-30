using Ghost.Graphics.Contracts;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Ghost.Graphics.DX12;

public unsafe class DX12Resource : IResource
{
    private readonly ID3D12Resource _nativeResource;

    internal ID3D12Resource NativeResource => _nativeResource;

    public ulong GPUAddress => _nativeResource.GPUVirtualAddress;

    public string Name
    {
        get => _nativeResource.Name;
        set => _nativeResource.Name = value;
    }

    public DX12Resource(ID3D12Resource nativeResource)
    {
        _nativeResource = nativeResource;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetData<T>(Span<T> data)
        where T : unmanaged
    {
        _nativeResource.WriteToSubresource(0, data, 0, 0);
    }

    public void Dispose()
    {
        _nativeResource.Dispose();
    }
}