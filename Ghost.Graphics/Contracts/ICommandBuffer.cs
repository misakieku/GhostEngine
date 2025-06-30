namespace Ghost.Graphics.Contracts;

public interface ICommandBuffer
{
    public void CopyResource(IResource dstResource, uint dstOffset, IResource srcResource, uint srcOffset, uint size);
}