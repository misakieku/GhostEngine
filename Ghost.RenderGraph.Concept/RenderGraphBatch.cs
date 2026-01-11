using System.Collections.Generic;

namespace Ghost.RenderGraph.Concept;

internal class RenderGraphBatch
{
    public int ID { get; private set; }
    public RenderQueueType QueueType { get; private set; }
    public List<RenderGraphPass> Passes { get; } = new();
    
    // Fences to wait on BEFORE executing this batch
    public List<int> WaitFences { get; } = new();
    
    // Fences to signal AFTER executing this batch
    public List<int> SignalFences { get; } = new();

    public RenderGraphBatch()
    {
    }

    public void Initialize(int id, RenderQueueType queueType)
    {
        ID = id;
        QueueType = queueType;
    }

    public void Reset()
    {
        Passes.Clear();
        WaitFences.Clear();
        SignalFences.Clear();
    }
}
