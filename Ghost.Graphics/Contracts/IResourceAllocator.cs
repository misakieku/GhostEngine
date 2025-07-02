using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Contracts;

internal unsafe interface IResourceAllocator : IDisposable
{
    public IResource CreateUploadBuffer(uint sizeInBytes, bool tempResource = false, ResourceFlags flags = ResourceFlags.None);
    public IResource CreateCopyDestinationBuffer(uint sizeInBytes, bool tempResource = false, ResourceFlags flags = ResourceFlags.None);

    public void ReleaseTempResource();
}