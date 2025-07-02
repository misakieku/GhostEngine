using Ghost.Graphics.Data;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Contracts;

public interface ICommandBuffer
{
    public void DrawMesh(Mesh mesh);
    public void CopyResource(IResource dstResource, uint dstOffset, IResource srcResource, uint srcOffset, uint size);
    public void BarrierTransition(IResource resource, ResourceStates beforeState, ResourceStates afterState);
}