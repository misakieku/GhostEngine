using System;
using System.Collections.Generic;

namespace Ghost.RenderGraph.Concept;

public enum RenderQueueType
{
    Graphics,
    Compute,
    AsyncCompute,
    Copy
}

internal abstract class RenderGraphPass
{
    public string Name { get; set; } = string.Empty;
    public int Index { get; set; }
    public RenderQueueType QueueType { get; set; }
    public List<(RenderGraphResourceHandle handle, ResourceState state)> ResourceAccesses { get; set; }
    public List<int> Dependencies { get; } = new();
    public int RefCount { get; set; } = 0;
    public bool AllowCulling { get; set; }

    protected RenderGraphPass(
        string name,
        int index,
        RenderQueueType queueType,
        List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses,
        bool allowCulling)
    {
        Name = name;
        Index = index;
        QueueType = queueType;
        ResourceAccesses = resourceAccesses;
        AllowCulling = allowCulling;
    }

    protected void InitializeBase(
        string name,
        int index,
        RenderQueueType queueType,
        List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses,
        bool allowCulling)
    {
        Name = name;
        Index = index;
        QueueType = queueType;
        ResourceAccesses = resourceAccesses;
        AllowCulling = allowCulling;
        Dependencies.Clear();
        RefCount = 0;
    }

    public abstract void Execute(ICommandBuffer commandBuffer);
    public abstract void Release();
}

internal static class RenderGraphPassPool<TPassData>
    where TPassData : class
{
    public static readonly Stack<RenderGraphPass<TPassData>> Pool = new();
}

internal class RenderGraphPass<TPassData> : RenderGraphPass
    where TPassData : class
{
    public TPassData PassData { get; private set; }
    public Action<TPassData, ICommandBuffer> RenderFunc { get; private set; }

    public RenderGraphPass(
        string name,
        int index,
        RenderQueueType queueType,
        TPassData passData,
        Action<TPassData, ICommandBuffer> renderFunc,
        List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses,
        bool allowCulling)
        : base(name, index, queueType, resourceAccesses, allowCulling)
    {
        PassData = passData;
        RenderFunc = renderFunc;
    }

    public void Initialize(
        string name,
        int index,
        RenderQueueType queueType,
        TPassData passData,
        Action<TPassData, ICommandBuffer> renderFunc,
        List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses,
        bool allowCulling)
    {
        InitializeBase(name, index, queueType, resourceAccesses, allowCulling);
        PassData = passData;
        RenderFunc = renderFunc;
    }

    public override void Execute(ICommandBuffer commandBuffer)
    {
        RenderFunc(PassData, commandBuffer);
    }

    public override void Release()
    {
        PassData = null!;
        RenderFunc = null!;
        // ResourceAccesses list ownership is transferred back to RenderGraph
        ResourceAccesses = null!; 
        RenderGraphPassPool<TPassData>.Pool.Push(this);
    }
}
