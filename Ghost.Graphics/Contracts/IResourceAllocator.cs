using Vortice.Direct3D12;

namespace Ghost.Graphics.Contracts;

internal unsafe interface IResourceAllocator : IDisposable
{
    public abstract static IResourceAllocator Create();

    public IResource CreateUploadBuffer(uint sizeInBytes, ResourceFlags flags = ResourceFlags.None);
    public IResource CreateCopyDestinationBuffer(uint sizeInBytes, ResourceFlags flags = ResourceFlags.None);
}