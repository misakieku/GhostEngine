namespace Ghost.RenderGraph.Concept;

internal class ResourceUsage
{
    public RenderGraphResourceHandle Handle { get; }
    public ResourceState State { get; }
    public int PassIndex { get; }

    public ResourceUsage(RenderGraphResourceHandle handle, ResourceState state, int passIndex)
    {
        Handle = handle;
        State = state;
        PassIndex = passIndex;
    }
}

internal class ResourceLifetime
{
    public RenderGraphResourceHandle Handle { get; }
    public int FirstUse { get; set; } = int.MaxValue;
    public int LastUse { get; set; } = -1;
    public List<ResourceUsage> Usages { get; } = new();

    public ResourceLifetime(RenderGraphResourceHandle handle)
    {
        Handle = handle;
    }

    public void AddUsage(ResourceState state, int passIndex)
    {
        Usages.Add(new ResourceUsage(Handle, state, passIndex));
        FirstUse = Math.Min(FirstUse, passIndex);
        LastUse = Math.Max(LastUse, passIndex);
    }
}
