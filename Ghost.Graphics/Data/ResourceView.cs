namespace Ghost.Graphics.Data;

internal abstract unsafe class ResourceView
{
    public GraphicsResource Resource
    {
        get;
    }

    internal void* CpuDescriptorHandle
    {
        get;
    }

    protected ResourceView(GraphicsResource resource, void* descriptor)
    {
        Resource = resource;
        CpuDescriptorHandle = descriptor;
    }

    protected ResourceView(GraphicsResource resource, IntPtr descriptor) :
        this(resource, (void*)descriptor)
    {
    }
}
