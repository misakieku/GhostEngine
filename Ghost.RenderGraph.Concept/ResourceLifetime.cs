namespace Ghost.RenderGraph.Concept;

internal struct ResourceUsage
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

internal struct ResourceLifetime
{
    public RenderGraphResourceHandle Handle { get; private set; }
    public int FirstUse { get; set; } = int.MaxValue;
    public int LastUse { get; set; } = -1;
    public List<ResourceUsage> Usages { get; } = new();

    public ResourceLifetime()
    {
    }

    public void Initialize(RenderGraphResourceHandle handle)
    {
        Handle = handle;
        FirstUse = int.MaxValue;
        LastUse = -1;
        Usages.Clear();
    }

    public void AddUsage(ResourceState state, int passIndex)
    {
        Usages.Add(new ResourceUsage(Handle, state, passIndex));
        FirstUse = Math.Min(FirstUse, passIndex);
        LastUse = Math.Max(LastUse, passIndex);
    }
}
