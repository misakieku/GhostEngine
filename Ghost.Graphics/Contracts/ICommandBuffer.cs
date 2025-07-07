using Ghost.Graphics.Data;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Contracts;

public interface ICommandBuffer
{
    // TODO: They should be internal, maybe an interface ICommandBufferInternal?
    public void BarrierTransition(IResource resource, ResourceStates beforeState, ResourceStates afterState);
    public void SetGraphicsRootConstantBufferView(uint slot, ulong gpuAddress);

    public void DrawMesh(Mesh mesh, Material material);
    public void CopyResource(IResource dstResource, uint dstOffset, IResource srcResource, uint srcOffset, uint size);
}