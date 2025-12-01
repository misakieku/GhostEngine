namespace Ghost.RenderGraph.Concept;

internal abstract class RenderGraphPass
{
    public string Name { get; }
    public int Index { get; }
    public List<(RenderGraphResourceHandle handle, ResourceState state)> ResourceAccesses { get; }
    public List<int> Dependencies { get; } = new();
    public int RefCount { get; set; } = 0;
    public bool AllowCulling { get; }

    protected RenderGraphPass(
        string name,
        int index,
        List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses,
        bool allowCulling)
    {
        Name = name;
        Index = index;
        ResourceAccesses = resourceAccesses;
        AllowCulling = allowCulling;
    }

    public abstract void Execute(ICommandBuffer commandBuffer);
}

internal class RenderGraphPass<TPassData> : RenderGraphPass
    where TPassData : class
{
    public TPassData PassData { get; }
    public Action<TPassData, ICommandBuffer> RenderFunc { get; }

    public RenderGraphPass(
        string name,
        int index,
        TPassData passData,
        Action<TPassData, ICommandBuffer> renderFunc,
        List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses,
        bool allowCulling)
        : base(name, index, resourceAccesses, allowCulling)
    {
        PassData = passData;
        RenderFunc = renderFunc;
    }

    public override void Execute(ICommandBuffer commandBuffer)
    {
        RenderFunc(PassData, commandBuffer);
    }
}
